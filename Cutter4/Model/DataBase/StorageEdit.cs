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
        public PanelCollection<StorageEditItem> StorageEditItems { get; }

        public StorageEdit(DateTime dateTime, string message, PanelCollection<StorageEditItem> storageEditItems)
        {
            DateTime = dateTime;
            Message = message;
            StorageEditItems = storageEditItems;
        }
    }

    public struct StorageEditItem
    {
        public int PanelLength { get; }
        public int DeltaCount { get; }

        public StorageEditItem(int panelLength, int deltaCount)
        {
            if (panelLength < 1)
                throw new ArgumentException(nameof(panelLength) + " can not be smaller than 1");
            PanelLength = panelLength;
            DeltaCount = deltaCount;
        }
    }
}
