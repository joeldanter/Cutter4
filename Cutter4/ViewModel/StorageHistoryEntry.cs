using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cutter4.Domain;

namespace Cutter4.ViewModel
{
    public interface IStorageHistoryEntry
    {
        string Summary { get; }
        string Details { get; }
        DateTime DateTime { get; }
    }

    public class EditStorageHistoryEntry : IStorageHistoryEntry
    {
        public string Summary { get; }
        public string Details { get; }
        public DateTime DateTime { get; }

        public EditStorageHistoryEntry(StorageEdit storageEdit)
        {
            Summary = $"Edited {storageEdit.StorageEditItems.Count} Panels";
            Details = detailsText(storageEdit);
            DateTime = storageEdit.Date;
        }

        string detailsText(StorageEdit storageEdit)
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine($"Date: {storageEdit.Date}");
            result.AppendLine();
            result.AppendLine("Message:");
            result.AppendLine(storageEdit.Message);
            result.AppendLine();
            result.AppendLine("Changes:");
             
            foreach (KeyValuePair<PanelKey, int> entry in storageEdit.StorageEditItems)
            {
                PanelColors color = entry.Key.Color;
                PanelWidths width = entry.Key.Width;
                int length = entry.Key.Length;
                int deltaCount = entry.Value;

                result.AppendLine($"{color} {width}: {deltaCount.ToString("+0;-0;0")} {length}mm");
            }
            return result.ToString();
        }
    }

    public class OrderSaveStorageHistoryEntry : IStorageHistoryEntry
    {
        public string Summary { get; }
        public string Details { get; }
        public DateTime DateTime { get; }
        public ReadOnlyCollection<Kapu> Kapuk { get; }

        public OrderSaveStorageHistoryEntry(Order order, string orderOutput)
        {
            Summary = $"Ordered {order.Kapuk.Count} Kapu";
            Details = orderOutput;
            DateTime = order.Date;
            Kapuk = order.Kapuk.ToList().AsReadOnly();
        }
    }
}
