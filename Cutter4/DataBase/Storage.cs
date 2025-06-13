using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cutter4.Cutting;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace Cutter4.DataBase
{
    class Storage
    {
        DBConnectionSingleton dbCon;
        public Storage()
        {
            dbCon = DBConnectionSingleton.Instance();
            dbCon.Server = "127.0.0.1";
            dbCon.DatabaseName = "cutter";
            dbCon.UserName = "main";
            dbCon.Password = "Almacitrom"; // igen tudom nagyon biztonságos
            loadColors();
        }

        void executeNonQueryWithTransaction(MySqlCommand command, MySqlTransaction transaction)
        {
            command.Connection = dbCon.Connection;
            command.Transaction = transaction;
            Console.WriteLine(command.CommandText + "\n");
            command.ExecuteNonQuery();
        }
        
        public void SaveNewCut(IList<Kapu> kapuk, PanelCollection<Configuration> confs)
        {
            dbCon.IsConnect();
            MySqlTransaction transaction = dbCon.Connection.BeginTransaction();
            
            try
            {
                saveNewOrder(out long orderId, transaction);
                saveKapuk(kapuk, orderId, transaction);
                RemoveUsedPanels(confs, transaction);
                AppendRemainderPanels(confs, transaction);
                SaveConfigurations(confs, orderId, out long[] orderedNewConfigurationIds, transaction);
                SaveCutElements(confs, orderedNewConfigurationIds, transaction);

                transaction.Rollback();
                Console.WriteLine("rolled back cuz testing");
                //transaction.Commit();
                //Console.WriteLine("Order successfully saved to database.");
            }
            catch (MySqlException e)
            {
                Console.WriteLine($"An exception of type {e.GetType()} was encountered while inserting the data:");
                Console.WriteLine(e.Message);
                try
                {
                    transaction.Rollback();
                    Console.WriteLine("Transaction successfully rolled back.");
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine($"An exception of type {ex.GetType()} was encountered while attempting to roll back the transaction:");
                    Console.WriteLine(ex.Message);
                }
                throw e;
            }
        }

        void saveNewOrder(out long orderId, MySqlTransaction transaction)
        {
            MySqlCommand command = new MySqlCommand("INSERT INTO orders (date) VALUES ROW(NOW());");
            executeNonQueryWithTransaction(command, transaction);
            orderId = command.LastInsertedId;
        }

        void saveKapuk(IList<Kapu> kapuk, long orderID, MySqlTransaction transaction)
        {
            MySqlCommand command = new MySqlCommand();

            List<string> valuesRows = new List<string>();
            for (int i = 0; i < kapuk.Count; i++)
            {
                Kapu kapu = kapuk[i];
                valuesRows.Add($"    ROW(@length{i}, @height{i}, @serial_number{i}, @color{i})");
                command.Parameters.Add($"@length{i}", MySqlDbType.Int16).Value = kapu.Length;
                command.Parameters.Add($"@height{i}", MySqlDbType.Int16).Value = kapu.Height;
                command.Parameters.Add($"@serial_number{i}", MySqlDbType.VarChar, 255).Value = kapu.SerialNumber;
                command.Parameters.Add($"@color{i}", MySqlDbType.VarChar, 255).Value = kapu.Color;
            }
            string valuesString = string.Join(",\n", valuesRows);

            string commandText = $@"
INSERT INTO kapuk (length, height, serial_number, color_id, order_id)
SELECT new.length, new.height, new.serial_number, colors.id, {orderID}
FROM (VALUES
{valuesString}
) AS new(length, height, serial_number, color_name)
LEFT JOIN colors
    ON new.color_name=colors.name;";

            command.CommandText = commandText;

            executeNonQueryWithTransaction(command, transaction);
        }

        void RemoveUsedPanels(PanelCollection<Configuration> confs, MySqlTransaction transaction)
        {
            MySqlCommand command = new MySqlCommand();

            List<string> valuesRows = new List<string>();
            int i = 0;
            foreach (KeyValuePair<PanelType, List<Configuration>> item in confs)
            {
                PanelType type = item.Key;
                foreach (Configuration conf in item.Value)
                {
                    valuesRows.Add($"    ROW(@length{i}, @color_name{i}, @width{i})");
                    command.Parameters.Add($"@length{i}", MySqlDbType.Int16).Value = conf.Capacity;
                    command.Parameters.Add($"@color_name{i}", MySqlDbType.VarChar, 255).Value = type.Color;
                    command.Parameters.Add($"@width{i}", MySqlDbType.Enum).Value = type.Width;
                    i++;
                }
            }
            string valuesString = string.Join(",\n", valuesRows);

            string commandText =
$@"UPDATE panels
LEFT JOIN panel_types
    ON panels.type_id=panel_types.id
LEFT JOIN colors
    ON panel_types.color_id=colors.id
RIGHT JOIN (VALUES
{valuesString}
) AS new(length, color_name, width)
    ON new.length=panels.length
    AND new.color_name=colors.name
    AND new.width=panel_types.width
SET panels.storage_count=panels.storage_count-1;";

            command.CommandText = commandText;
            executeNonQueryWithTransaction(command, transaction);
        }

        void AppendRemainderPanels(PanelCollection<Configuration> confs, MySqlTransaction transaction)
        {
            MySqlCommand command = new MySqlCommand();
            
            List<string> valuesRows = new List<string>();
            int i = 0;
            foreach (KeyValuePair<PanelType, List<Configuration>> item in confs)
            {
                PanelType type = item.Key;
                foreach (Configuration conf in item.Value)
                {
                    valuesRows.Add($"    ROW(@length{i}, @color_name{i}, @width{i})");
                    command.Parameters.Add($"@length{i}", MySqlDbType.Int16).Value = conf.Waste;
                    command.Parameters.Add($"@color_name{i}", MySqlDbType.VarChar, 255).Value = type.Color;
                    command.Parameters.Add($"@width{i}", MySqlDbType.Enum).Value = type.Width;
                    i++;
                }
            }
            string valuesString = string.Join(",\n", valuesRows);

            string commandText =
$@"INSERT INTO panels (length, type_id, storage_count)
SELECT new.length, panel_types.id, 1
FROM (VALUES
{valuesString}
) AS new(length, color_name, width)
LEFT JOIN colors
    ON new.color_name=colors.name
LEFT JOIN panel_types
    ON colors.id=panel_types.color_id
    AND new.width=panel_types.width
    ON DUPLICATE KEY UPDATE panels.storage_count=panels.storage_count+1;";

            command.CommandText = commandText;
            executeNonQueryWithTransaction(command, transaction);
        }

        void SaveConfigurations(PanelCollection<Configuration> confs, long orderID, out long[] orderedNewConfigurationIds, MySqlTransaction transaction)
        {
            MySqlCommand command = new MySqlCommand();

            List<string> valuesRows = new List<string>();
            int i = 0;
            foreach (KeyValuePair<PanelType, List<Configuration>> item in confs)
            {
                PanelType type = item.Key;
                foreach (Configuration conf in item.Value)
                {
                    valuesRows.Add($"    ROW(@original_length{i}, @color_name{i}, @width{i})");
                    command.Parameters.Add($"@original_length{i}", MySqlDbType.Int16).Value = conf.Capacity;
                    command.Parameters.Add($"@color_name{i}", MySqlDbType.VarChar, 255).Value = type.Color;
                    command.Parameters.Add($"@width{i}", MySqlDbType.Enum).Value = type.Width;
                    i++;
                }
            }
            string valuesString = string.Join(",\n", valuesRows);

            string commandText =
$@"INSERT INTO cut_configurations (order_id, original_length, type_id)
SELECT {orderID}, new.original_length, panel_types.id
FROM (VALUES
{valuesString}
) AS new (original_length, color_name, width)
LEFT JOIN colors
    ON new.color_name=colors.name
LEFT JOIN panel_types
    ON colors.id=panel_types.color_id
    AND new.width=panel_types.width;";

            command.CommandText = commandText;
            executeNonQueryWithTransaction(command, transaction);

            int lastConfId = (int)command.LastInsertedId;
            int no_confs = confs.Sum(x => x.Value.Count);
            orderedNewConfigurationIds = new long[no_confs];
            for (long saved_conf_i = 0; saved_conf_i < no_confs; saved_conf_i++)
            {
                orderedNewConfigurationIds[saved_conf_i] = lastConfId + saved_conf_i;
            }
        }

        void SaveCutElements(PanelCollection<Configuration> confs, long[] orderedNewConfigurationIds, MySqlTransaction transaction)
        {
            MySqlCommand command = new MySqlCommand();

            List<string> valuesRows = new List<string>();
            int conf_i = 0, panel_i = 0;
            foreach (KeyValuePair<PanelType, List<Configuration>> item in confs)
            {
                PanelType type = item.Key;
                foreach (Configuration conf in item.Value)
                {
                    foreach (Panel panel in conf.Panels)
                    {
                        valuesRows.Add($"    ROW(@serial_number{panel_i}, @configuration_id{panel_i})");
                        command.Parameters.Add($"@serial_number{panel_i}", MySqlDbType.VarChar, 255).Value = panel.SerialNumber;
                        command.Parameters.Add($"@configuration_id{panel_i}", MySqlDbType.Int64).Value = orderedNewConfigurationIds[conf_i];
                        panel_i++;
                    }
                    conf_i++;
                }
            }
            string valuesString = string.Join(",\n", valuesRows);

            string commandText =
$@"INSERT INTO cut_elements (kapu_id, configuration_id)
SELECT kapuk.id, new.configuration_id
FROM (VALUES
{valuesString}
) AS new (serial_number, configuration_id)
LEFT JOIN kapuk
    ON new.serial_number=kapuk.serial_number";

            command.CommandText = commandText;
            executeNonQueryWithTransaction(command, transaction);
        }

        public List<StoragePanel> LeftOvers(PanelType type)
        {
            dbCon.IsConnect();
            MySqlCommand command = new MySqlCommand();
            string query = @"
                SELECT length, storage_count
                FROM panels
                INNER JOIN panel_types
                    ON panels.type_id = panel_types.id
                INNER JOIN colors
                    ON panel_types.color_id = colors.id
                WHERE storage_count != 0
                    AND colors.name = @color
                    AND panel_types.width = @width;";
            command.CommandText = query;
            command.Parameters.Add("@color", MySqlDbType.VarChar, 255).Value = type.Color;
            command.Parameters.Add("@width", MySqlDbType.Enum).Value = type.Width;
            command.Connection = dbCon.Connection;
            
            MySqlDataAdapter adapter = new MySqlDataAdapter(command);
            DataSet dataSet = new DataSet();
            adapter.Fill(dataSet);

            List<StoragePanel> storagePanels = new List<StoragePanel>();
            foreach (DataRow row in dataSet.Tables[0].Rows)
            {
                int length = Convert.ToInt32(row["length"]);
                int count = Convert.ToInt32(row["storage_count"]);
                storagePanels.Add(new StoragePanel(length, count));
            }

            return storagePanels;
        }

        void loadColors()
        {
            dbCon.IsConnect();
            MySqlDataAdapter adapter = new MySqlDataAdapter("SELECT (name) FROM colors;", dbCon.Connection);
            DataSet dataSet = new DataSet();
            adapter.Fill(dataSet);
            Colors.ColorNames = new List<string>();

            foreach (DataRow item in dataSet.Tables[0].Rows)
            {
                Colors.ColorNames.Add(item["name"].ToString());
            }
        }
    }
}
