using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace Cutter4.DataBase
{
    public class DBConnectionSingleton
    {
        private DBConnectionSingleton()
        {
        }

        public string Server { get; set; }
        public string DatabaseName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public MySqlConnection Connection { get; set; }

        private static DBConnectionSingleton _instance = null;
        public static DBConnectionSingleton Instance()
        {
            if (_instance == null)
                _instance = new DBConnectionSingleton();
            return _instance;
        }

        public void IsConnect()
        {
            if (Connection == null)
            {
                if (string.IsNullOrEmpty(DatabaseName))
                    throw new ArgumentException();
                string connstring = string.Format("Server={0}; database={1}; UID={2}; password={3}", Server, DatabaseName, UserName, Password);
                Connection = new MySqlConnection(connstring);
                Connection.Open();
            }
        }

        public void Close()
        {
            Connection.Close();
        }
    }
}
