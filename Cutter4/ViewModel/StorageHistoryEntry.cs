using Cutter4.Model.Cutting;
using Cutter4.Model.DataBase;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            Summary = $"Edited {storageEdit.DeltaCounts.Count} Panels";
            Details = detailsText(storageEdit);
            DateTime = storageEdit.DateTime;
        }

        string detailsText(StorageEdit storageEdit)
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine($"Date: {storageEdit.DateTime}");
            result.AppendLine();
            result.AppendLine("Message:");
            result.AppendLine(storageEdit.Message);
            result.AppendLine();
            result.AppendLine("Changes:");

            foreach (KeyValuePair<Panel, int> entry in storageEdit.DeltaCounts)
            {
                PanelType type = entry.Key.Type;
                int length = entry.Key.Length;
                int deltaCount = entry.Value;

                result.AppendLine($"{type.Color} {type.Width}: {deltaCount.ToString("+0;-0;0")} {length}mm");
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
            DateTime = order.DateTime;
            Kapuk = new ReadOnlyCollection<Kapu>(order.Kapuk);
        }
    }
}
