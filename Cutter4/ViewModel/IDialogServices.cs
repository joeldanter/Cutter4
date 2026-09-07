using Cutter4.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cutter4.ViewModel
{
    public interface IMessageDialogService
    {
        void ShowError(string message);
        void ShowInfo(string message);
    }

    public interface IConfigurationLayoutDialogService
    {
        Dictionary<PanelType, List<Configuration>>? ShowDialog(Dictionary<PanelType, List<Configuration>> confs, IDictionary<PanelKey, int> storage);
    }

    public interface IEditStorageDialogService
    {
        bool ShowDialog(IDictionary<PanelKey, int> storage);
    }

    public interface ITruckDeliveryDialogService
    {
        int? ShowDialog();
    }
}
