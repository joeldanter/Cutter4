using Cutter4.Model.Cutting;
using Cutter4.Model.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cutter4.ViewModel
{
    public interface IStorageAction
    {
        string Summary { get; set; }
        string Details { get; set; }
        DateTime DateTime { get; set; }
    }

    public class StorageEditAction : IStorageAction
    {
        public string Summary { get; set; }
        public string Details { get; set; }
        public DateTime DateTime { get; set; }

        public StorageEditAction(StorageEdit storageEdit)
        {
            Summary = (storageEdit.DeltaCount < 0 ? "Removed" : "Added") + $" {Math.Abs(storageEdit.DeltaCount)} panel" + (Math.Abs(storageEdit.DeltaCount) == 1 ? "" : "s");
            Details = $"{storageEdit.DeltaCount.ToString("+0;-0;0")} {storageEdit.PanelLength}mm {storageEdit.PanelColor} {storageEdit.PanelWidth}";
            DateTime = storageEdit.DateTime;
        }
    }

    public class OrderSaveAction : IStorageAction
    {
        public string Summary { get; set; }
        public string Details { get; set; }
        public DateTime DateTime { get; set; }

        public OrderSaveAction(Order order, string orderOutput)
        {
            Summary = $"Ordered {order.Kapuk.Count} Kapu";
            Details = orderOutput;
            DateTime = order.DateTime;
        }
    }
}
