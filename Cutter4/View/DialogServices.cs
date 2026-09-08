using Cutter4.Domain;
using Cutter4.Persistence.Repositories;
using Cutter4.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Windows;

namespace Cutter4.View
{
    internal class MessageBoxDialogService : IMessageDialogService
    {
        public void ShowError(string message)
            => MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        public void ShowInfo(string message)
            => MessageBox.Show(message, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public class ConfigurationLayoutDialogService : IConfigurationLayoutDialogService
    {
        IMessageDialogService dialogService;
        ILocalizationService localizationService;

        public ConfigurationLayoutDialogService(IMessageDialogService dialogService, ILocalizationService localizationService)
        {
            this.dialogService = dialogService;
            this.localizationService = localizationService;
        }

        public Dictionary<PanelType, List<Configuration>>? ShowDialog(Dictionary<PanelType, List<Configuration>> confs, IDictionary<PanelKey, int> storage)
        {
            ConfigurationLayoutViewModel vm = new(dialogService, localizationService, confs, storage);
            ConfigurationLayoutWindow window = new(vm)
            {
                Owner = WindowHelper.GetActiveWindow(),
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            return window.ShowDialog() == true ? vm.Result : null;
        }
    }

    public class EditStorageDialogService : IEditStorageDialogService
    {
        IStorageRepository storageRepository;
        IMessageDialogService dialogService;
        ILocalizationService localizationService;
        ITruckDeliveryDialogService truckDeliveryDialogService;

        public EditStorageDialogService(IStorageRepository storageRepository, IMessageDialogService dialogService, ILocalizationService localizationService, ITruckDeliveryDialogService truckDeliveryDialogService)
        {
            this.storageRepository = storageRepository;
            this.dialogService = dialogService;
            this.localizationService = localizationService;
            this.truckDeliveryDialogService = truckDeliveryDialogService;
        }

        public bool ShowDialog(IDictionary<PanelKey, int> storage)
        {
            EditStorageViewModel vm = new(storageRepository, dialogService, localizationService, truckDeliveryDialogService);
            EditStorageWindow window = new(vm)
            {
                Owner = WindowHelper.GetActiveWindow(),
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            return window.ShowDialog() == true;
        }
    }

    public class TruckDeliveryDialogService : ITruckDeliveryDialogService
    {
        public int? ShowDialog()
        {
            TruckDeliveryViewModel vm = new();
            TruckDeliveryWindow window = new(vm)
            {
                Owner = WindowHelper.GetActiveWindow(),
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            return window.ShowDialog() == true ? vm.CountToImport : null;
        }
    }

    public static class WindowHelper
    {
        public static Window? GetActiveWindow()
        {
            return Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.IsActive)
                ?? Application.Current.MainWindow;
        }
    }
}
