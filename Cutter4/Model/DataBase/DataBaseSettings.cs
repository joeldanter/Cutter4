using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cutter4.Model.DataBase
{
    public class DataBaseSettings(string server, string databaseName, string userName, string password)
    {
        public string Server { get; set; } = server;
        public string DatabaseName { get; set; } = databaseName;
        public string UserName { get; set; } = userName;
        public string Password { get; set; } = password;
    }
}
