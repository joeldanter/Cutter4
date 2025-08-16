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
    public interface IStorageAction
    {
        string Summary { get; }
        string Details { get; }
        DateTime DateTime { get; }
    }

    public class StorageEditAction : IStorageAction
    {
        public string Summary { get; }
        public string Details { get; }
        public DateTime DateTime { get; }

        public StorageEditAction(StorageEdit storageEdit)
        {
            Summary = $"Edited {storageEdit.StorageEditItems.ToList().Sum(entry => entry.Value.Count)} Panels";
            Details = detailsText(storageEdit);
            DateTime = storageEdit.DateTime;
        }

        string detailsText(StorageEdit storageEdit)
        {
            string result = $"{storageEdit.DateTime}\n\n{storageEdit.Message}\n";
            foreach (KeyValuePair<PanelType, List<StorageEditItem>> entry in storageEdit.StorageEditItems)
            {
                PanelType type = entry.Key;
                foreach (StorageEditItem item in entry.Value)
                {
                    result += $"\n{item.DeltaCount.ToString("+0;-0;0")} {item.PanelLength}mm {type.Color} {type.Width}";
                }
            }
            return result;
        }
    }

    public class OrderSaveAction : IStorageAction
    {
        public string Summary { get; }
        public string Details { get; }
        public DateTime DateTime { get; }
        public ReadOnlyCollection<Kapu> Kapuk { get; }

        public OrderSaveAction(Order order, string orderOutput)
        {
            Summary = $"Ordered {order.Kapuk.Count} Kapu";
            Details = orderOutput;
            DateTime = order.DateTime;
            Kapuk = new ReadOnlyCollection<Kapu>(order.Kapuk);
        }
    }
}
