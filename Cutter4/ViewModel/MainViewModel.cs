using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cutter4.Model.Cutting;
using Cutter4.Model.DataBase;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cutter4.ViewModel
{
    public partial class MainViewModel : ObservableObject
    {
        Solver solver;
        Storage storage;

        [ObservableProperty]
        ObservableCollection<Kapu> kapuk;
        [ObservableProperty]
        string outputText;

        PanelCollection<StoragePanel> storagePanels;
        public List<StoragePanel> StoragePanels500mm { get => storagePanels[new PanelType("500mm", SelectedPanelColor)]; }
        public List<StoragePanel> StoragePanels610mm { get => storagePanels[new PanelType("610mm", SelectedPanelColor)]; }
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StoragePanels500mm))]
        [NotifyPropertyChangedFor(nameof(StoragePanels610mm))]
        PanelColors selectedPanelColor;

        [ObservableProperty]
        string server = "127.0.0.1";
        [ObservableProperty]
        string databaseName = "cutter";
        [ObservableProperty]
        string userName = "main";
        [ObservableProperty]
        string password = "Almacitrom";

        public MainViewModel()
        {
            solver = new Solver();
            storage = new Storage();
            SaveDatabaseSettings();
            storage.StoragePanelsChanged += Storage_StoragePanelsChanged;
            
            kapuk = new ObservableCollection<Kapu>();
            outputText = string.Empty;

            storagePanels = storage.StoragePanels();
            selectedPanelColor = PanelColors.AF500;
        }

        private void Storage_StoragePanelsChanged(object sender, EventArgs e)
        {
            storagePanels = storage.StoragePanels();
            OnPropertyChanged(nameof(StoragePanels500mm));
            OnPropertyChanged(nameof(StoragePanels610mm));
        }

        [RelayCommand]
        void Calculate()
        {
            PanelCollection<KapuPanel> panelCollectionToCut = Kapu.ProcessPanels(Kapuk);
            PanelCollection<Configuration> confs = solver.Solve(panelCollectionToCut, storage.StoragePanels());
            OutputText = solver.GenerateOutput(Kapuk, confs);
        }

        [RelayCommand]
        void Save()
        {
            throw new NotImplementedException();
        }

        [RelayCommand]
        void NewCut()
        {
            Kapuk.Clear();
            OutputText = string.Empty;
        }

        [RelayCommand]
        void SaveDatabaseSettings()
        {
            storage.Server = Server;
            storage.DatabaseName = DatabaseName;
            storage.UserName = UserName;
            storage.Password = Password;
        }

        [RelayCommand]
        void CancelDatabaseSettings()
        {
            Server = storage.Server;
            DatabaseName = storage.DatabaseName;
            UserName =storage.UserName;
            Password = storage.Password;
        }
    }
}
