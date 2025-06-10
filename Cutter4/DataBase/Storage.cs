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
    public class Storage
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

        DataSet QueryExecutora(string query)
        {
            Console.WriteLine("query: " + query);
            dbCon.IsConnect();
            DataSet dataSet = new DataSet();
            MySqlDataAdapter adapter = new MySqlDataAdapter(query, dbCon.Connection);
            adapter.Fill(dataSet);
            return dataSet;
        }

        long CommandExecutora(string query)
        {
            Console.WriteLine("command: " + query);
            dbCon.IsConnect();
            MySqlCommand cmd = new MySqlCommand(query, dbCon.Connection);
            cmd.ExecuteNonQuery();
            return cmd.LastInsertedId;
        }

        void CreateCommandAndTrans(out MySqlCommand command, out MySqlTransaction transaction)
        {
            dbCon.IsConnect();
            command = dbCon.Connection.CreateCommand();
            transaction = dbCon.Connection.BeginTransaction();
            command.Connection = dbCon.Connection;
            command.Transaction = transaction;
        }

        public void BetterSave(IList<Kapu> kapuk, PanelCollection<Configuration> confs)
        {
            CreateCommandAndTrans(out MySqlCommand myCommand, out MySqlTransaction myTrans);

            try
            {
                // save new order
                myCommand.CommandText = saveNewOrderQuery();
                Console.WriteLine(myCommand.CommandText);
                myCommand.ExecuteNonQuery();
                long orderID = myCommand.LastInsertedId;

                // save kapuk
                myCommand.CommandText = saveKapukQuery(kapuk, orderID);
                Console.WriteLine(myCommand.CommandText);
                myCommand.ExecuteNonQuery();

                // remove used panels
                myCommand.CommandText = RemoveUsedPanelsQuery(confs);
                Console.WriteLine(myCommand.CommandText);
                myCommand.ExecuteNonQuery();

                // add new panels
                myCommand.CommandText = AddRemainderPanelsQuery(confs);
                Console.WriteLine(myCommand.CommandText);
                myCommand.ExecuteNonQuery();

                // add configurations
                myCommand.CommandText = SaveConfigurationsQuery(confs, orderID);
                Console.WriteLine(myCommand.CommandText);
                myCommand.ExecuteNonQuery();

                // save cut elements
                //myCommand.CommandText = SaveCutElementsQuery(kapuk, confs);
                //Console.WriteLine(myCommand.CommandText);
                //myCommand.ExecuteNonQuery();

                myTrans.Rollback();
                //myTrans.Commit();
                Console.WriteLine("Order successfully saved to database.");
            }
            catch (ArgumentException e)
            {
                try
                {
                    myTrans.Rollback();
                }
                catch (MySqlException ex)
                {
                    if (myTrans.Connection != null)
                    {
                        Console.WriteLine("An exception of type " + ex.GetType() +
                        " was encountered while attempting to roll back the transaction.");
                    }
                }
                Console.WriteLine("An exception of type " + e.GetType() +
                " was encountered while inserting the data.");
                Console.WriteLine("Transaction successfully rolled back.");
                throw e;
            }
        }

        string saveNewOrderQuery()
        {
            return "INSERT INTO orders (date) VALUES ROW(NOW());";
        }

        string saveKapukQuery(IList<Kapu> kapuk, long orderID)
        {
            string kapuRows = "";
            foreach (Kapu kapu in kapuk)
            {
                kapuRows += $"ROW({kapu.Length}, {kapu.Height}, '{kapu.SerialNumber}', '{kapu.Color}'),\n";
            }
            kapuRows = kapuRows.Substring(0, kapuRows.Length - 2);

            return
$@"INSERT INTO kapuk (length, height, serial_number, color_id, order_id)
SELECT new.length, new.height, new.serial_number, colors.id, {orderID}
FROM (VALUES
{kapuRows}
) AS new(length, height, serial_number, color_name)
LEFT JOIN colors
    ON new.color_name=colors.name;";
        }

        string RemoveUsedPanelsQuery(PanelCollection<Configuration> confs)
        {
            string usedPanelRows = "";
            foreach (KeyValuePair<PanelType, List<Configuration>> item in confs)
            {
                PanelType type = item.Key;
                foreach (Configuration conf in item.Value)
                {
                    usedPanelRows += $"ROW({conf.Capacity}, '{type.Color}', '{type.Width}'),\n";
                }
            }
            usedPanelRows = usedPanelRows.Substring(0, usedPanelRows.Length - 2);

            return
$@"UPDATE panels
LEFT JOIN panel_types
    ON panels.type_id=panel_types.id
LEFT JOIN colors
    ON panel_types.color_id=colors.id
RIGHT JOIN (VALUES
{usedPanelRows}
) AS new(length, color_name, width)
    ON new.length=panels.length
    AND new.color_name=colors.name
    AND new.width=panel_types.width
SET panels.storage_count=panels.storage_count-1;";
        }

        string AddRemainderPanelsQuery(PanelCollection<Configuration> confs)
        {
            string remainderPanelRows = "";
            foreach (KeyValuePair<PanelType, List<Configuration>> item in confs)
            {
                PanelType type = item.Key;
                foreach (Configuration conf in item.Value)
                {
                    remainderPanelRows += $"ROW({conf.Waste}, '{type.Color}', '{type.Width}'),\n";
                }
            }
            remainderPanelRows = remainderPanelRows.Substring(0, remainderPanelRows.Length - 2);

            return
$@"INSERT INTO panels (length, type_id, storage_count)
SELECT new.length, panel_types.id, 1
FROM (VALUES
{remainderPanelRows}
) AS new(length, color_name, width)
LEFT JOIN colors
    ON new.color_name=colors.name
LEFT JOIN panel_types
    ON colors.id=panel_types.color_id
    AND new.width=panel_types.width
    ON DUPLICATE KEY UPDATE panels.storage_count=panels.storage_count+1;";
        }

        string SaveConfigurationsQuery(PanelCollection<Configuration> confs, long orderID)
        {
            string confRows = "";
            foreach (KeyValuePair<PanelType, List<Configuration>> item in confs)
            {
                PanelType type = item.Key;
                foreach (Configuration conf in item.Value)
                {
                    confRows += $"ROW({conf.Capacity}, {conf.Waste}, '{type.Color}', '{type.Width}'),\n";
                }
            }
            confRows = confRows.Substring(0, confRows.Length - 2);

            return
$@"INSERT INTO cut_configurations (order_id, used_length, remainder_length, type_id)
SELECT {orderID}, new.used_length, new.remainder_length, panel_types.id
FROM (VALUES
{confRows}
) AS new (used_length, remainder_length, color_name, width)
LEFT JOIN colors
    ON new.color_name=colors.name
LEFT JOIN panel_types
    ON colors.id=panel_types.color_id
    AND new.width=panel_types.width;";
        }

        string SaveCutElementsQuery(IList<Kapu> kapuk, PanelCollection<Configuration> confs)
        {
            throw new NotImplementedException();
        }

        public DataSet LeftOvers(PanelType type)
        {
            string color = type.Color;
            string width = type.Width;
            string query = $"SELECT length, storage_count FROM panels INNER JOIN panel_types ON panels.type_id = panel_types.id INNER JOIN colors ON panel_types.color_id = colors.id WHERE storage_count != 0 AND colors.name = '{color}' AND panel_types.width = '{width}';";
            dbCon.IsConnect();
            DataSet dataSet = new DataSet();
            MySqlDataAdapter adapter = new MySqlDataAdapter(query, dbCon.Connection);
            adapter.Fill(dataSet);
            return dataSet;
        }

        void loadColors()
        {
            dbCon.IsConnect();
            DataSet dataSet = new DataSet();
            MySqlDataAdapter adapter = new MySqlDataAdapter("SELECT (name) FROM colors;", dbCon.Connection);
            adapter.Fill(dataSet);
            Colors.ColorNames = new List<string>();

            foreach (DataRow item in dataSet.Tables[0].Rows)
            {
                Colors.ColorNames.Add(item["name"].ToString());
            }
        }
    }
}
