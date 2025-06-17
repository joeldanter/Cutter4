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
        public string Server { get; set; }
        public string DatabaseName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        string connString { get { return $"Server={Server}; database={DatabaseName}; UID={UserName}; password={Password}"; } }

        public Storage() {  }

        long executeNonQueryWithTransaction(string commandText, MySqlConnection connection, MySqlTransaction transaction, params MySqlParameter[] parameters)
        {
            Console.WriteLine(commandText + "\n");

            MySqlCommand command = new MySqlCommand()
            {
                CommandText = commandText,
                CommandType = CommandType.Text,
                Connection = connection,
                Transaction = transaction
            };

            if (parameters != null)
                foreach (MySqlParameter p in parameters)
                    command.Parameters.Add(p);

            command.ExecuteNonQuery();
            command.Parameters.Clear();

            return command.LastInsertedId;
        }
        
        public void SaveNewCut(IList<Kapu> kapuk, PanelCollection<Configuration> confs)
        {
            MySqlConnection connection = new MySqlConnection(connString);
            connection.Open();
            MySqlTransaction transaction = connection.BeginTransaction();
            
            try
            {
                saveNewOrder(out long orderId, connection, transaction);
                saveKapuk(kapuk, orderId, connection, transaction);
                removeUsedPanels(confs, connection, transaction);
                appendRemainderPanels(confs, connection, transaction);
                saveConfigurations(confs, orderId, out long[] orderedNewConfigurationIds, connection, transaction);
                saveCutElements(confs, orderedNewConfigurationIds, connection, transaction);

                transaction.Commit();
                Console.WriteLine("Order successfully saved to database.");
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
                    throw ex;
                }
                throw e;
            }
        }

        void saveNewOrder(out long orderId, MySqlConnection connection, MySqlTransaction transaction)
        {
            string commandText = "INSERT INTO orders (date) VALUES ROW(NOW());";
            orderId = executeNonQueryWithTransaction(commandText, connection, transaction);
        }

        void saveKapuk(IList<Kapu> kapuk, long orderID, MySqlConnection connection, MySqlTransaction transaction)
        {
            List<MySqlParameter> parameters = new List<MySqlParameter>();
            List<string> valuesRows = new List<string>();
            for (int i = 0; i < kapuk.Count; i++)
            {
                Kapu kapu = kapuk[i];
                valuesRows.Add($"    ROW(@length{i}, @height{i}, @serial_number{i}, @color{i})");
                parameters.Add(new MySqlParameter($"@length{i}", MySqlDbType.Int16) { Value = kapu.Length });
                parameters.Add(new MySqlParameter($"@height{i}", MySqlDbType.Int16) { Value = kapu.Height });
                parameters.Add(new MySqlParameter($"@serial_number{i}", MySqlDbType.VarChar, 255) { Value = kapu.SerialNumber });
                parameters.Add(new MySqlParameter($"@color{i}", MySqlDbType.VarChar, 255) { Value = kapu.Color });
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

            executeNonQueryWithTransaction(commandText, connection, transaction, parameters.ToArray());
        }

        void removeUsedPanels(PanelCollection<Configuration> confs, MySqlConnection connection, MySqlTransaction transaction)
        {
            List<MySqlParameter> parameters = new List<MySqlParameter>();
            List<string> valuesRows = new List<string>();
            int i = 0;
            foreach (KeyValuePair<PanelType, List<Configuration>> item in confs)
            {
                PanelType type = item.Key;
                foreach (Configuration conf in item.Value)
                {
                    valuesRows.Add($"    ROW(@length{i}, @color_name{i}, @width{i})");
                    parameters.Add(new MySqlParameter($"@length{i}", MySqlDbType.Int16) { Value = conf.Capacity });
                    parameters.Add(new MySqlParameter($"@color_name{i}", MySqlDbType.VarChar, 255) { Value = type.Color });
                    parameters.Add(new MySqlParameter($"@width{i}", MySqlDbType.Enum) { Value = type.Width });
                    i++;
                }
            }
            string valuesString = string.Join(",\n", valuesRows);

            string commandText =
$@"UPDATE panels
LEFT JOIN colors
    ON panels.color_id=colors.id
RIGHT JOIN (VALUES
{valuesString}
) AS new(length, color_name, width)
    ON new.length=panels.length
    AND new.color_name=colors.name
    AND new.width=panels.width
SET panels.storage_count=panels.storage_count-1;";

            executeNonQueryWithTransaction(commandText, connection, transaction, parameters.ToArray());
        }

        void appendRemainderPanels(PanelCollection<Configuration> confs, MySqlConnection connection, MySqlTransaction transaction)
        {
            List<MySqlParameter> parameters = new List<MySqlParameter>();
            List<string> valuesRows = new List<string>();
            int i = 0;
            foreach (KeyValuePair<PanelType, List<Configuration>> item in confs)
            {
                PanelType type = item.Key;
                foreach (Configuration conf in item.Value)
                {
                    valuesRows.Add($"    ROW(@length{i}, @color_name{i}, @width{i})");
                    parameters.Add(new MySqlParameter($"@length{i}", MySqlDbType.Int16) { Value = conf.Waste });
                    parameters.Add(new MySqlParameter($"@color_name{i}", MySqlDbType.VarChar, 255) { Value = type.Color });
                    parameters.Add(new MySqlParameter($"@width{i}", MySqlDbType.Enum) { Value = type.Width });
                    i++;
                }
            }
            string valuesString = string.Join(",\n", valuesRows);

            string commandText =
$@"INSERT INTO panels (length, color_id, width, storage_count)
SELECT new.length, colors.id, new.width, 1
FROM (VALUES
{valuesString}
) AS new(length, color_name, width)
LEFT JOIN colors
    ON new.color_name=colors.name
ON DUPLICATE KEY UPDATE panels.storage_count=panels.storage_count+1;";

            executeNonQueryWithTransaction(commandText, connection, transaction, parameters.ToArray());
        }

        void saveConfigurations(PanelCollection<Configuration> confs, long orderID, out long[] orderedNewConfigurationIds, MySqlConnection connection, MySqlTransaction transaction)
        {
            List<MySqlParameter> parameters = new List<MySqlParameter>();
            List<string> valuesRows = new List<string>();
            int i = 0;
            foreach (KeyValuePair<PanelType, List<Configuration>> item in confs)
            {
                PanelType type = item.Key;
                foreach (Configuration conf in item.Value)
                {
                    valuesRows.Add($"    ROW(@original_length{i}, @color_name{i}, @width{i})");
                    parameters.Add(new MySqlParameter($"@original_length{i}", MySqlDbType.Int16) { Value = conf.Capacity });
                    parameters.Add(new MySqlParameter($"@color_name{i}", MySqlDbType.VarChar, 255) { Value = type.Color });
                    parameters.Add(new MySqlParameter($"@width{i}", MySqlDbType.Enum) { Value = type.Width });
                    i++;
                }
            }
            string valuesString = string.Join(",\n", valuesRows);

            string commandText =
$@"INSERT INTO cut_configurations (order_id, used_panel_id)
SELECT {orderID}, panels.id
FROM (VALUES
{valuesString}
) AS new (original_length, color_name, width)
LEFT JOIN colors
    ON new.color_name=colors.name
LEFT JOIN panels
    ON new.original_length=panels.length
    AND colors.id=panels.color_id
    AND new.width=panels.width;";

            long lastConfId = executeNonQueryWithTransaction(commandText, connection, transaction, parameters.ToArray());

            int no_confs = confs.Sum(x => x.Value.Count);
            orderedNewConfigurationIds = new long[no_confs];
            for (int saved_conf_i = 0; saved_conf_i < no_confs; saved_conf_i++)
            {
                orderedNewConfigurationIds[saved_conf_i] = lastConfId + saved_conf_i;
            }
        }

        void saveCutElements(PanelCollection<Configuration> confs, long[] orderedNewConfigurationIds, MySqlConnection connection, MySqlTransaction transaction)
        {
            List<MySqlParameter> parameters = new List<MySqlParameter>();
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
                        parameters.Add(new MySqlParameter($"@serial_number{panel_i}", MySqlDbType.VarChar, 255) { Value = panel.SerialNumber });
                        parameters.Add(new MySqlParameter($"@configuration_id{panel_i}", MySqlDbType.Int64) { Value = orderedNewConfigurationIds[conf_i] });
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

            executeNonQueryWithTransaction(commandText, connection, transaction, parameters.ToArray());
        }

        public void UndoLastSavedCut()
        {
            MySqlConnection connection = new MySqlConnection(connString);
            connection.Open();
            MySqlTransaction transaction = connection.BeginTransaction();

            // TODO need checking
            // TODO add try catch
            unAppendLastOrderRemainderPanels(connection, transaction);
            unRemoveLastOrderUsedPanels(connection, transaction);
            unSaveLastOrderAndDetails(connection, transaction);
            
            transaction.Commit();
            Console.WriteLine("Last saved cut was successfully undone from the database.");
        }

        void unAppendLastOrderRemainderPanels(MySqlConnection connection, MySqlTransaction transaction)
        {
            // TODO ez egy haborus bun amilyen csunya es nem mukodik jol
            string commandText = @"
UPDATE panels
RIGHT JOIN (
    SELECT *, COUNT(*) AS count
    FROM (
        SELECT orders.date AS order_date, 13200-SUM(kapuk.length) AS length, panels.color_id, panels.width
        FROM cut_elements
        LEFT JOIN kapuk
            ON cut_elements.kapu_id=kapuk.id
        LEFT JOIN cut_configurations
            ON cut_elements.configuration_id=cut_configurations.id
        LEFT JOIN panels
            ON cut_configurations.used_panel_id=panels.id
        LEFT JOIN orders
            ON cut_configurations.order_id=orders.id
        GROUP BY cut_configurations.id
    ) AS idk
    GROUP BY idk.order_date, idk.length, idk.color_id, idk.width
) AS remainder_panels
    ON panels.length = remainder_panels.length
    AND panels.width = remainder_panels.width
    AND panels.color_id = remainder_panels.color_id
SET panels.storage_count=panels.storage_count-remainder_panels.count;
WHERE remainder_panels.order_date = (SELECT MAX(date) FROM orders LIMIT 1);";

            executeNonQueryWithTransaction(commandText, connection, transaction);
        }

        void unRemoveLastOrderUsedPanels(MySqlConnection connection, MySqlTransaction transaction)
        {
            string commandText = @"
UPDATE panels
RIGHT JOIN cut_configurations
    ON cut_configurations.used_panel_id=panels.id
LEFT JOIN orders
    ON cut_configurations.order_id=orders.id
SET storage_count=storage_count+1
WHERE orders.date = (SELECT MAX(date) FROM orders LIMIT 1);
";

            executeNonQueryWithTransaction(commandText, connection, transaction);
        }

        void unSaveLastOrderAndDetails(MySqlConnection connection, MySqlTransaction transaction)
        {
            // thanks to ON DELETE CASCADE properties other tables' rows get deleted as well, so this is all that is needed
            string commandText = @"
DELETE FROM orders
WHERE date = (SELECT MAX(date) FROM orders LIMIT 1);";

            executeNonQueryWithTransaction(commandText, connection, transaction);
        }

        public List<StoragePanel> LeftOvers(PanelType type)
        {
            MySqlCommand command = new MySqlCommand();
            string query = @"
                SELECT panels.length, panels.storage_count
                FROM panels
                LEFT JOIN colors
                    ON panels.color_id = colors.id
                WHERE storage_count != 0
                    AND colors.name = @color
                    AND panels.width = @width;";
            command.CommandText = query;
            command.Parameters.Add("@color", MySqlDbType.VarChar, 255).Value = type.Color;
            command.Parameters.Add("@width", MySqlDbType.Enum).Value = type.Width;
            command.Connection = new MySqlConnection(connString);
            
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
    }
}
