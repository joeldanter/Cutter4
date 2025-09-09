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
        public Dictionary<StorageEditEntry, int> DeltaCounts { get; }

        public StorageEdit(DateTime dateTime, string message, Dictionary<StorageEditEntry, int> deltaCounts)
        {
            DateTime = dateTime;
            Message = message;
            DeltaCounts = deltaCounts;
        }
    }
    
    public readonly record struct StorageEditEntry(PanelType Type, int Length);
}
