using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cutter4.Model.Cutting;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace Cutter4.Model.DataBase
{
    public class Storage
    {
        public string Server { get; set; } 
        public string DatabaseName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        string connString
        {
            get
            {
                MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder()
                {
                    Server=Server,
                    Database=DatabaseName,
                    UserID=UserName,
                    Password=Password
                };
                
                return builder.ConnectionString;
            }
        }

        public event EventHandler StoragePanelsChanged;
        public event EventHandler OrderListChanged;
        public event EventHandler StorageEditsListChanged;

        void OnStoragePanelsChanged()
        {
            StoragePanelsChanged?.Invoke(this, EventArgs.Empty);
        }
        
        void OnOrderListChanged()
        {
            OrderListChanged?.Invoke(this, EventArgs.Empty);
        }

        void OnStorageEditsListChanged()
        {
            StorageEditsListChanged?.Invoke(this, EventArgs.Empty);
        }

        public Storage() {  }

        public bool CanConnect()
        {
            using (MySqlConnection conn = new MySqlConnection(connString))
            {
                try
                {
                    conn.Open();
                    return conn.Ping();
                }
                catch (MySqlException)
                {
                    return false;
                }
            }
        }

        long executeNonQueryWithTransaction(string commandText, MySqlConnection connection, MySqlTransaction transaction, params MySqlParameter[] parameters)
        {
            using (MySqlCommand command = new MySqlCommand(commandText, connection, transaction))
            {
                command.CommandType = CommandType.Text;

                if (parameters != null)
                    foreach (MySqlParameter p in parameters)
                        command.Parameters.Add(p);

                command.ExecuteNonQuery();
                command.Parameters.Clear();

                return command.LastInsertedId;
            }
        }

        DataSet executeQuery(string connString, string query, params MySqlParameter[] parameters)
        {
            using (MySqlConnection connection = new MySqlConnection(connString))
            using (MySqlCommand command = new MySqlCommand(query, connection))
            using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
            {
                if (parameters != null)
                    foreach (MySqlParameter p in parameters)
                        command.Parameters.Add(p);

                DataSet dataSet = new DataSet();
                adapter.Fill(dataSet);
                return dataSet;
            }
        }

        #region SaveNewCut
        public void SaveNewCut(IList<Kapu> kapuk, PanelCollection<Configuration> confs)
        {
            raiseExceptionIfCantRemoveUsedPanels(confs);
            raiseExceptionIfCantSaveKapuk(kapuk);

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
            }
            catch
            {
                try
                {
                    transaction.Rollback();
                }
                catch
                {
                    throw;
                }
                throw;
            }

            OnStoragePanelsChanged();
            OnOrderListChanged();
        }

        void raiseExceptionIfCantRemoveUsedPanels(PanelCollection<Configuration> confs)
        {
            List<MySqlParameter> parameters = new List<MySqlParameter>();
            List<string> valuesRows = new List<string>();
            int i = 0;
            foreach (KeyValuePair<PanelType, List<Configuration>> item in confs)
            {
                PanelType type = item.Key;
                foreach (Configuration conf in item.Value)
                {
                    valuesRows.Add($"        ROW(@length{i}, @color_name{i}, @width{i})");
                    parameters.Add(new MySqlParameter($"@length{i}", MySqlDbType.Int16) { Value = conf.Capacity });
                    parameters.Add(new MySqlParameter($"@color_name{i}", MySqlDbType.VarChar, 255) { Value = type.Color });
                    parameters.Add(new MySqlParameter($"@width{i}", MySqlDbType.Enum) { Value = type.Width });
                    i++;
                }
            }
            string valuesString = string.Join(",\n", valuesRows);

            string commandText = $@"
SELECT panels.length, colors.name AS color_name, panels.width, panels.storage_count, updates.reduce_by
FROM panels
JOIN colors
    ON panels.color_id = colors.id
RIGHT JOIN (
    SELECT length, color_name, width, COUNT(*) as reduce_by
    FROM (VALUES
{valuesString}
    ) AS new(length, color_name, width)
    GROUP BY length, color_name, width
) AS updates
    ON panels.length = updates.length
    AND colors.name = updates.color_name
    AND panels.width = updates.width
WHERE panels.storage_count < updates.reduce_by;";
            DataSet dataSet = executeQuery(connString, commandText, parameters.ToArray());

            if (dataSet.Tables[0].Rows.Count > 0)
            {
                string msg = "The order cannot be saved because there are not enough panels in storage to cut from using the given configurations:";
                foreach (DataRow row in dataSet.Tables[0].Rows)
                {
                    int length = Convert.ToInt32(row["length"]);
                    Enum.TryParse(row["color_name"].ToString(), out PanelColors color);
                    string width = row["width"].ToString();
                    int storageCount = Convert.ToInt32(row["storage_count"]);
                    int reduceBy = Convert.ToInt32(row["reduce_by"]);
                    msg += $"\n{length}mm {color} {width}: at least {reduceBy} are needed, but only {storageCount} are in storage";
                }
                throw new InvalidOperationException(msg);
            }
        }

        void raiseExceptionIfCantSaveKapuk(IList<Kapu> kapuk)
        {
            List<MySqlParameter> parameters = new List<MySqlParameter>();
            List<string> valuesRows = new List<string>();
            for (int i = 0; i < kapuk.Count; i++)
            {
                Kapu kapu = kapuk[i];
                valuesRows.Add($"    ROW(@kapu{i})");
                parameters.Add(new MySqlParameter($"@kapu{i}", MySqlDbType.VarChar, 255) { Value = kapu.SerialNumber });
            }
            string valuesString = string.Join(",\n", valuesRows);

            string commandText = $@"
SELECT kapuk.serial_number
FROM ( VALUES
{valuesString}
) AS new(serial_number)
JOIN kapuk
    ON new.serial_number=kapuk.serial_number;";
            DataSet dataSet = executeQuery(connString, commandText, parameters.ToArray());

            if (dataSet.Tables[0].Rows.Count > 0)
            {
                string msg = "The order cannot be saved because there is already a previous kapu saved with the following serial number(s): ";
                List<string> serialNumbers = new List<string>();
                foreach (DataRow row in dataSet.Tables[0].Rows)
                {
                    string serial_number = row["serial_number"].ToString();
                    serialNumbers.Add(serial_number);
                }
                msg += string.Join(", ", serialNumbers);
                throw new InvalidOperationException(msg);
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
                    valuesRows.Add($"        ROW(@length{i}, @color_name{i}, @width{i})");
                    parameters.Add(new MySqlParameter($"@length{i}", MySqlDbType.Int16) { Value = conf.Capacity });
                    parameters.Add(new MySqlParameter($"@color_name{i}", MySqlDbType.VarChar, 255) { Value = type.Color });
                    parameters.Add(new MySqlParameter($"@width{i}", MySqlDbType.Enum) { Value = type.Width });
                    i++;
                }
            }
            string valuesString = string.Join(",\n", valuesRows);

            string commandText =
$@"UPDATE panels
JOIN colors ON panels.color_id = colors.id
JOIN (
    SELECT length, color_name, width, COUNT(*) as reduce_by
    FROM (VALUES
{valuesString}
    ) AS new(length, color_name, width)
    GROUP BY length, color_name, width
) AS updates
    ON panels.length = updates.length
    AND colors.name = updates.color_name
    AND panels.width = updates.width
SET panels.storage_count = panels.storage_count - updates.reduce_by;";

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
                    foreach (KapuPanel panel in conf.Panels)
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
        #endregion

        #region UndoLastSavedCut
        public void UndoLastSavedCut()
        {
            raiseExceptionIfCantUnAppendLastOrderRemainderPanels();
            MySqlConnection connection = new MySqlConnection(connString);
            connection.Open();
            MySqlTransaction transaction = connection.BeginTransaction();

            try
            {
                unAppendLastOrderRemainderPanels(connection, transaction);
                unRemoveLastOrderUsedPanels(connection, transaction);
                unSaveLastOrderAndDetails(connection, transaction);
                
                transaction.Commit();
            }
            catch
            {
                try
                {
                    transaction.Rollback();
                }
                catch
                {
                    throw;
                }
                throw;
            }

            OnStoragePanelsChanged();
            OnOrderListChanged();
        }
        
        void raiseExceptionIfCantUnAppendLastOrderRemainderPanels()
        {
            string query = $@"
SELECT panels.length, colors.name AS color_name, panels.width, panels.storage_count, remainder_panels.count AS use_count
FROM panels
RIGHT JOIN (
    SELECT *, COUNT(*) AS count
    FROM (
        SELECT orders.date AS order_date, panels.length-SUM(kapuk.length) AS length, panels.color_id, panels.width
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
LEFT JOIN colors
    ON remainder_panels.color_id=colors.id
WHERE panels.storage_count<remainder_panels.count
    AND remainder_panels.order_date = (SELECT MAX(date) FROM orders LIMIT 1);";
            DataSet dataSet = executeQuery(connString, query);
            DataRowCollection rows = dataSet.Tables[0].Rows;
            if (rows.Count > 0)
            {
                string msg = "The order cannot be reverted because there are not enough panels in storage to revert the creation of remainder panels produced by the cut:";
                foreach (DataRow row in dataSet.Tables[0].Rows)
                {
                    int length = Convert.ToInt32(row["length"]);
                    Enum.TryParse(row["color_name"].ToString(), out PanelColors color);
                    string width = row["width"].ToString();
                    int storageCount = Convert.ToInt32(row["storage_count"]);
                    int useCount = Convert.ToInt32(row["use_count"]);
                    msg += $"\n{length}mm {color} {width}: {useCount} would need to be reverted, but only {storageCount} are in storage";
                }
                throw new InvalidOperationException(msg);
            }
        }

        void unAppendLastOrderRemainderPanels(MySqlConnection connection, MySqlTransaction transaction)
        {
            // TODO ez egy haborus bun amilyen csunya
            string commandText = @"
UPDATE panels
RIGHT JOIN (
    SELECT *, COUNT(*) AS count
    FROM (
        SELECT orders.date AS order_date, panels.length-SUM(kapuk.length) AS length, panels.color_id, panels.width
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
SET panels.storage_count=panels.storage_count-remainder_panels.count
WHERE remainder_panels.order_date = (SELECT MAX(date) FROM orders LIMIT 1);";

            executeNonQueryWithTransaction(commandText, connection, transaction);
        }

        void unRemoveLastOrderUsedPanels(MySqlConnection connection, MySqlTransaction transaction)
        {
            string commandText = @"
UPDATE panels
RIGHT JOIN (
    SELECT cut_configurations.used_panel_id, COUNT(*) AS count
    FROM cut_configurations
    LEFT JOIN orders
        ON cut_configurations.order_id=orders.id
    WHERE orders.date = (SELECT MAX(date) FROM orders LIMIT 1)
    GROUP BY cut_configurations.used_panel_id
) AS sub ON panels.id = sub.used_panel_id
SET panels.storage_count = panels.storage_count + sub.count;
";

            executeNonQueryWithTransaction(commandText, connection, transaction);
        }

        void unSaveLastOrderAndDetails(MySqlConnection connection, MySqlTransaction transaction)
        {
            // thanks to ON DELETE CASCADE properties other tables' rows get deleted as well, so this is all that is needed
            string commandText = @"
DELETE FROM orders
WHERE id = (
    SELECT id FROM (
        SELECT id FROM orders
        ORDER BY date DESC
        LIMIT 1
    ) AS temp
);";

            executeNonQueryWithTransaction(commandText, connection, transaction);
        }
        #endregion

        public PanelCollection<StoragePanel> StoragePanels()
        {
            string query = @"
                SELECT colors.name, panels.width, panels.length, panels.storage_count
                FROM panels
                LEFT JOIN colors
                    ON panels.color_id = colors.id
                WHERE storage_count != 0;";
            DataSet dataSet = executeQuery(connString, query);

            PanelCollection<StoragePanel> storagePanels = new PanelCollection<StoragePanel>();
            foreach (DataRow row in dataSet.Tables[0].Rows)
            {
                Enum.TryParse(row["name"].ToString(), out PanelColors color);
                string width = row["width"].ToString();
                int length = Convert.ToInt32(row["length"]);
                int count = Convert.ToInt32(row["storage_count"]);

                PanelType type = new PanelType(width, color);
                StoragePanel storagePanel = new StoragePanel(length, count);
                storagePanels[type].Add(storagePanel);
            }

            return storagePanels;
        }

        public List<StoragePanel> StoragePanelsOfType(PanelType type)
        {
            string query = $@"
                SELECT panels.length, panels.storage_count
                FROM panels
                LEFT JOIN colors
                    ON panels.color_id = colors.id
                WHERE storage_count != 0
                    AND colors.name = ""{type.Color}""
                    AND panels.width = ""{type.Width}"";";
            DataSet dataSet = executeQuery(connString, query);
            
            List<StoragePanel> storagePanels = new List<StoragePanel>();
            foreach (DataRow row in dataSet.Tables[0].Rows)
            {
                int length = Convert.ToInt32(row["length"]);
                int count = Convert.ToInt32(row["storage_count"]);
                storagePanels.Add(new StoragePanel(length, count));
            }

            return storagePanels;
        }

        #region EditStoragePanels
        public void EditStoragePanels(string message, PanelCollection<StorageEditItem> edits)
        {
            raiseExceptionIfCantEditStoragePanels(edits);

            MySqlConnection connection = new MySqlConnection(connString);
            connection.Open();
            MySqlTransaction transaction = connection.BeginTransaction();
            
            try
            {
                // too much code in one method
                // meh idc
                // also this aint efficient cuz we do the same group by 3 times
                // but i dont wanna refactor again

                List<MySqlParameter> parameters = new List<MySqlParameter>();
                List<string> valuesRows = new List<string>();
                int i = 0;
                foreach (KeyValuePair<PanelType, List<StorageEditItem>> entry in edits)
                {
                    PanelType type = entry.Key;
                    foreach (StorageEditItem editItem in entry.Value)
                    {
                        valuesRows.Add($"        ROW(@length{i}, @color{i}, @width{i}, @delta_count{i})");
                        parameters.Add(new MySqlParameter($"@length{i}", MySqlDbType.Int16) { Value = editItem.PanelLength });
                        parameters.Add(new MySqlParameter($"@color{i}", MySqlDbType.VarChar, 255) { Value = type.Color.ToString() });
                        parameters.Add(new MySqlParameter($"@width{i}", MySqlDbType.Enum) { Value = type.Width });
                        parameters.Add(new MySqlParameter($"@delta_count{i}", MySqlDbType.Int16) { Value = editItem.DeltaCount });
                        i++;
                    }
                }
                string valuesString = string.Join(",\n", valuesRows);

                string updatePanelsCommandText = $@"
INSERT IGNORE INTO panels (length, color_id, width, storage_count)
SELECT new.length, colors.id, new.width, 0
FROM ( VALUES
{valuesString}
) AS new(length, color, width, delta_count)
LEFT JOIN colors
    ON new.color=colors.name;

UPDATE panels
LEFT JOIN colors
    ON panels.color_id=colors.id
RIGHT JOIN (
    SELECT length, color, width, SUM(delta_count) AS sum_delta_count
    FROM ( VALUES
{valuesString}
    ) AS new(length, color, width, delta_count)
    GROUP BY length, color, width
) AS new(length, color, width, sum_delta_count)
    ON panels.length=new.length
    AND colors.name=new.color
    AND panels.width=new.width
SET panels.storage_count=panels.storage_count+new.sum_delta_count;";
                executeNonQueryWithTransaction(updatePanelsCommandText, connection, transaction, parameters.ToArray());

                string insertEditsCommandText = $@"
INSERT INTO storage_edits (date, message)
VALUES ( NOW(), @message);";
                MySqlParameter messageParameter = new MySqlParameter("@message", MySqlDbType.VarChar, 255) { Value = message };
                long editID = executeNonQueryWithTransaction(insertEditsCommandText, connection, transaction, messageParameter);

                string insertEditItemsCommandText = $@"
INSERT INTO storage_edit_items (edit_id, panel_id, delta_count)
SELECT @edit_id, panels.id, new.sum_delta_count
FROM (
    SELECT length, color, width, SUM(delta_count) AS sum_delta_count
    FROM ( VALUES
{valuesString}
    ) AS new(length, color, width, delta_count)
    GROUP BY length, color, width
) AS new(length, color, width, sum_delta_count)
LEFT JOIN colors
    ON new.color=colors.name
LEFT JOIN panels
    ON new.length=panels.length
    AND colors.id=panels.color_id
    AND new.width=panels.width
WHERE new.sum_delta_count!=0;";
                MySqlParameter editIdParameter = new MySqlParameter("@edit_id", MySqlDbType.Int16) { Value = editID };
                executeNonQueryWithTransaction(insertEditItemsCommandText, connection, transaction, parameters.Append(editIdParameter).ToArray());
                
                transaction.Commit();
            }
            catch
            {
                try
                {
                    transaction.Rollback();
                }
                catch
                {
                    throw;
                }
                throw;
            }

            OnStoragePanelsChanged();
            OnStorageEditsListChanged();
        }

        void raiseExceptionIfCantEditStoragePanels(PanelCollection<StorageEditItem> edits)
        {
            List<MySqlParameter> parameters = new List<MySqlParameter>();
            List<string> valuesRows = new List<string>();
            int i = 0;
            foreach (KeyValuePair<PanelType, List<StorageEditItem>> entry in edits)
            {
                PanelType type = entry.Key;
                foreach (StorageEditItem editItem in entry.Value)
                {
                    valuesRows.Add($"        ROW(@length{i}, @color{i}, @width{i}, @delta_count{i})");
                    parameters.Add(new MySqlParameter($"@length{i}", MySqlDbType.Int16) { Value = editItem.PanelLength });
                    parameters.Add(new MySqlParameter($"@color{i}", MySqlDbType.VarChar, 255) { Value = type.Color.ToString() });
                    parameters.Add(new MySqlParameter($"@width{i}", MySqlDbType.Enum) { Value = type.Width });
                    parameters.Add(new MySqlParameter($"@delta_count{i}", MySqlDbType.Int16) { Value = editItem.DeltaCount });
                    i++;
                }
            }
            string valuesString = string.Join(",\n", valuesRows);

            string commandText = $@"
SELECT panels.storage_count, new.sum_delta_count, new.length, new.color, new.width
FROM (
    SELECT length, color, width, SUM(delta_count) AS sum_delta_count
    FROM ( VALUES
{valuesString}
    ) AS new(length, color, width, delta_count)
    GROUP BY length, color, width
) AS new(length, color, width, sum_delta_count)
LEFT JOIN colors
    ON new.color=colors.name
LEFT JOIN panels
    ON new.length=panels.length
    AND colors.id=panels.color_id
    AND new.width=panels.width
WHERE IFNULL(panels.storage_count, 0) < -new.sum_delta_count;";
            DataSet dataSet = executeQuery(connString, commandText, parameters.ToArray());

            if (dataSet.Tables[0].Rows.Count > 0)
            {
                string msg = "Can not remove more panels from storage than there are:";
                foreach (DataRow row in dataSet.Tables[0].Rows)
                {
                    int storage_count = Convert.ToInt32(row["storage_count"] == DBNull.Value ? 0 : row["storage_count"]);
                    int delta_count = Convert.ToInt32(row["sum_delta_count"]);
                    int length = Convert.ToInt32(row["length"]);
                    Enum.TryParse(row["color"].ToString(), out PanelColors color);
                    string width = row["width"].ToString();
                    msg += $"\n{length}mm {color} {width}: Tried to remove {-delta_count}, but only {storage_count} are in storage";
                }
                throw new InvalidOperationException(msg);
            }
        }
        #endregion

        public List<Order> ListPreviousOrders()
        {
            string query = $@"
SELECT id, date FROM orders
ORDER BY date DESC;";
            DataSet orderDatesDataSet = executeQuery(connString, query);

            Dictionary<long, Order> ordersByIds = new Dictionary<long, Order>();
            foreach (DataRow row in orderDatesDataSet.Tables[0].Rows)
            {
                long order_id = Convert.ToInt64(row["id"]);
                DateTime date = (DateTime)row["date"];
                Order order = new Order(date, new List<Kapu>(), new PanelCollection<Configuration>());
                ordersByIds.Add(order_id, order);
            }

            query = $@"
SELECT orders.id AS order_id, kapuk.length AS kapu_length, kapuk.height AS kapu_height, kapuk.serial_number, colors.name AS color_name
FROM kapuk
LEFT JOIN colors
    ON kapuk.color_id = colors.id
INNER JOIN orders
    ON kapuk.order_id = orders.id;";
            DataSet kapukDataSet = executeQuery(connString, query);

            foreach (DataRow row in kapukDataSet.Tables[0].Rows)
            {
                long order_id = Convert.ToInt64(row["order_id"]);
                int length = Convert.ToInt32(row["kapu_length"]);
                int height = Convert.ToInt32(row["kapu_height"]);
                Enum.TryParse(row["color_name"].ToString(), out PanelColors color);
                string serial_number = row["serial_number"].ToString();

                Kapu kapu = new Kapu(length, height, color, serial_number);
                ordersByIds[order_id].Kapuk.Add(kapu);
            }

            query = $@"
SELECT orders.id AS order_id, cut_configurations.id as configuration_id, panels.length as used_length, colors.name AS color_name, panels.width
FROM cut_configurations
LEFT JOIN panels
    ON cut_configurations.used_panel_id=panels.id
LEFT JOIN colors
    ON panels.color_id = colors.id
INNER JOIN orders
    ON cut_configurations.order_id = orders.id;";
            DataSet confsDataSet = executeQuery(connString, query);

            Dictionary<long, Configuration> confsByIds = new Dictionary<long, Configuration>();
            foreach (DataRow row in confsDataSet.Tables[0].Rows)
            {
                long order_id = Convert.ToInt64(row["order_id"]);
                long configuration_id = Convert.ToInt64(row["configuration_id"]);
                int used_length = Convert.ToInt32(row["used_length"]);
                Enum.TryParse(row["color_name"].ToString(), out PanelColors color);
                string width = row["width"].ToString();

                PanelType type = new PanelType(width, color);
                Configuration configuration = new Configuration(new List<KapuPanel>(), used_length);
                confsByIds.Add(configuration_id, configuration);
                ordersByIds[order_id].Configurations[type].Add(configuration);
            }

            query = $@"
SELECT cut_configurations.id AS configuration_id, kapuk.length AS cut_length, kapuk.serial_number
FROM cut_elements
LEFT JOIN kapuk
    ON cut_elements.kapu_id = kapuk.id
LEFT JOIN cut_configurations
    ON cut_elements.configuration_id = cut_configurations.id
INNER JOIN orders
    ON cut_configurations.order_id = orders.id;";
            DataSet CutElementsDataSet = executeQuery(connString, query);

            foreach (DataRow row in CutElementsDataSet.Tables[0].Rows)
            {
                long configuration_id = Convert.ToInt64(row["configuration_id"]);
                int cut_length = Convert.ToInt32(row["cut_length"]);
                string serial_number = row["serial_number"].ToString();

                KapuPanel kapuPanel = new KapuPanel(cut_length, serial_number);
                confsByIds[configuration_id].Panels.Add(kapuPanel);
            }

            return ordersByIds.Values.ToList();
        }

        public List<StorageEdit> ListPreviousStorageEdits()
        {
            string editsQuery = $@"
SELECT storage_edits.id, storage_edits.date, storage_edits.message
FROM storage_edits;";
            DataSet editsDataSet = executeQuery(connString, editsQuery);

            Dictionary<long, StorageEdit> editsByIds = new Dictionary<long, StorageEdit>();
            foreach (DataRow row in editsDataSet.Tables[0].Rows)
            {
                long edit_id = Convert.ToInt64(row["id"]);
                DateTime date = Convert.ToDateTime(row["date"]);
                string message = row["message"].ToString();

                StorageEdit newEdit = new StorageEdit(date, message, new PanelCollection<StorageEditItem>());
                editsByIds.Add(edit_id, newEdit);
            }

            string editItemsQuery = $@"
SELECT storage_edits.id AS edit_id, panels.length AS panel_length, colors.name AS panel_color, panels.width AS panel_width, storage_edit_items.delta_count
FROM storage_edit_items
LEFT JOIN storage_edits
    ON storage_edit_items.edit_id=storage_edits.id
LEFT JOIN panels
    ON storage_edit_items.panel_id=panels.id
LEFT JOIN colors
    ON panels.color_id=colors.id;";
            DataSet editItemsDataSet = executeQuery(connString, editItemsQuery);

            foreach (DataRow row in editItemsDataSet.Tables[0].Rows)
            {
                long edit_id = Convert.ToInt64(row["edit_id"]);
                int panel_length = Convert.ToInt32(row["panel_length"]);
                Enum.TryParse(row["panel_color"].ToString(), out PanelColors panel_color);
                string panel_width = row["panel_width"].ToString();
                int delta_count = Convert.ToInt32(row["delta_count"]);

                PanelType type = new PanelType(panel_width, panel_color);
                StorageEditItem editItem = new StorageEditItem(panel_length, delta_count);
                editsByIds[edit_id].StorageEditItems[type].Add(editItem);
            }

            return editsByIds.Values.ToList();
        }
    }
}
