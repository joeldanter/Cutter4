using Cutter4.Model.Cutting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cutter4.Model.DataBase
{
    public class StorageEdit
    {
        public DateTime DateTime { get; }
        public string Message { get; }
        public Dictionary<Panel, int> DeltaCounts { get; }

        public StorageEdit(DateTime dateTime, string message, Dictionary<Panel, int> deltaCounts)
        {
            DateTime = dateTime;
            Message = message;
            DeltaCounts = deltaCounts;
        }
    }
}
