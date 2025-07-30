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
            Summary = (storageEdit.DeltaCount < 0 ? "Removed" : "Added") + $" {Math.Abs(storageEdit.DeltaCount)} panel" + (Math.Abs(storageEdit.DeltaCount) == 1 ? "" : "s");
            Details = $"{storageEdit.DeltaCount.ToString("+0;-0;0")} {storageEdit.PanelLength}mm {storageEdit.PanelColor} {storageEdit.PanelWidth}";
            DateTime = storageEdit.DateTime;
        }
    }

    public class OrderSaveAction : IStorageAction
    {
        public string Summary { get; }
        public string Details { get; }
        public DateTime DateTime { get; }
        public ReadOnlyCollection<Kapu> Kapuk { get; }

        public OrderSaveAction(Order order, IList<Kapu> kapuk, string orderOutput)
        {
            Summary = $"Ordered {order.Kapuk.Count} Kapu";
            Details = orderOutput;
            DateTime = order.DateTime;
            Kapuk = new ReadOnlyCollection<Kapu>(kapuk);
        }
    }
}
