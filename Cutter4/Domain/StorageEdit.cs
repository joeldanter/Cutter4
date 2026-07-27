using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Cutter4.Domain
{
    public class StorageEdit
    {
        public DateTime Date { get; }
        public string Message { get; }
        public ReadOnlyDictionary<PanelKey, int> StorageEditItems { get; }

        public StorageEdit(DateTime date, string message, IDictionary<PanelKey, int> storageEditItems)
        {
            if (storageEditItems.Values.Any(c => c == 0))
                throw new ArgumentException("Tried to create a neutral change");

            Date = date;
            Message = message;
            StorageEditItems = storageEditItems.AsReadOnly();
        }
    }
}
