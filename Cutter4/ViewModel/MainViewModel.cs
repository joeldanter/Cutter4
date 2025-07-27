using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cutter4.Model.Cutting;
using Cutter4.Model.DataBase;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cutter4.ViewModel
{
    public partial class MainViewModel : ObservableObject
    {
        Solver solver = new Solver();
        Storage storage = new Storage();

        public FullyObservableCollection<KapuViewModel> Kapuk { get; } = new FullyObservableCollection<KapuViewModel>();
        [ObservableProperty]
        string outputText = string.Empty;
        bool canCalculate { get => Kapuk.Count != 0 && Kapuk.All(kapu => !kapu.HasErrors); }
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        bool canSave = false;

        PanelCollection<StoragePanel> storagePanels;
        public List<StoragePanel> StoragePanels500mm { get => storagePanels[new PanelType("500mm", SelectedStorageColor)]; }
        public List<StoragePanel> StoragePanels610mm { get => storagePanels[new PanelType("610mm", SelectedStorageColor)]; }
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StoragePanels500mm))]
        [NotifyPropertyChangedFor(nameof(StoragePanels610mm))]
        PanelColors selectedStorageColor = PanelColors.AF500;
        [ObservableProperty]
        StoragePanel? selected500mmStoragePanel;
        [ObservableProperty]
        StoragePanel? selected610mmStoragePanel;
        List<Kapu> calculatedKapuk;
        PanelCollection<Configuration> calculatedConfs;

        [ObservableProperty]
        string storagePanelToAdd500Length = string.Empty;
        [ObservableProperty]
        string storagePanelToAdd500Count = string.Empty;
        public Action SelectStoragePanelToAdd500Count { get; set; }
        [ObservableProperty]
        string storagePanelToAdd610Length = string.Empty;
        [ObservableProperty]
        string storagePanelToAdd610Count = string.Empty;
        public Action SelectStoragePanelToAdd610Count { get; set; }

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
            SaveDatabaseSettings();
            storage.StoragePanelsChanged += Storage_StoragePanelsChanged;
            storagePanels = storage.StoragePanels();

            Kapuk.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => { CanSave = false; CalculateCommand.NotifyCanExecuteChanged(); };
            Kapuk.ElementsPropertyChanged += (object sender, ElementPropertyChangedEventArgs e) => { CanSave = false; CalculateCommand.NotifyCanExecuteChanged(); };
        }

        private void Storage_StoragePanelsChanged(object sender, EventArgs e)
        {
            storagePanels = storage.StoragePanels();
            OnPropertyChanged(nameof(StoragePanels500mm));
            OnPropertyChanged(nameof(StoragePanels610mm));
        }

        [RelayCommand(CanExecute = nameof(canCalculate))]
        void Calculate()
        {
            calculatedKapuk = Kapuk.Select(kapu => kapu.ActualKapu()).ToList();
            PanelCollection<KapuPanel> panelCollectionToCut = Kapu.ProcessPanels(calculatedKapuk);
            calculatedConfs = solver.Solve(panelCollectionToCut, storage.StoragePanels());
            OutputText = solver.GenerateOutput(calculatedKapuk, calculatedConfs);
            CanSave = true;
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        void Save()
        {
            storage.SaveNewCut(calculatedKapuk, calculatedConfs);
            CanSave = false;
        }

        [RelayCommand]
        void NewCut()
        {
            Kapuk.Clear();
            OutputText = string.Empty;
        }

        [RelayCommand]
        void DoubleClicked500mmStoragePanel()
        {
            StoragePanelToAdd500Length = Selected500mmStoragePanel?.Length.ToString();
            Selected500mmStoragePanel = null;
            SelectStoragePanelToAdd500Count();
        }

        [RelayCommand]
        void DoubleClicked610mmStoragePanel()
        {
            StoragePanelToAdd610Length = Selected610mmStoragePanel?.Length.ToString();
            Selected610mmStoragePanel = null;
            SelectStoragePanelToAdd610Count();
        }

        [RelayCommand]
        void Add500mmStoragePanel()
        {
            PanelType type = new PanelType("500mm", SelectedStorageColor);
            int length = Convert.ToInt32(StoragePanelToAdd500Length);
            int count = Convert.ToInt32(StoragePanelToAdd500Count);
            storage.ChangeStoragePanelCount(type, length, count);
            StoragePanelToAdd500Length = string.Empty;
            StoragePanelToAdd500Count = string.Empty;
        }

        [RelayCommand]
        void Remove500mmStoragePanel()
        {
            PanelType type = new PanelType("500mm", SelectedStorageColor);
            int length = Convert.ToInt32(StoragePanelToAdd500Length);
            int count = Convert.ToInt32(StoragePanelToAdd500Count);
            storage.ChangeStoragePanelCount(type, length, -count);
            StoragePanelToAdd500Length = string.Empty;
            StoragePanelToAdd500Count = string.Empty;
        }

        [RelayCommand]
        void Add610mmStoragePanel()
        {
            PanelType type = new PanelType("610mm", SelectedStorageColor);
            int length = Convert.ToInt32(StoragePanelToAdd610Length);
            int count = Convert.ToInt32(StoragePanelToAdd610Count);
            storage.ChangeStoragePanelCount(type, length, count);
            StoragePanelToAdd610Length = string.Empty;
            StoragePanelToAdd610Count = string.Empty;
        }

        [RelayCommand]
        void Remove610mmStoragePanel()
        {
            PanelType type = new PanelType("610mm", SelectedStorageColor);
            int length = Convert.ToInt32(StoragePanelToAdd610Length);
            int count = Convert.ToInt32(StoragePanelToAdd610Count);
            storage.ChangeStoragePanelCount(type, length, -count);
            StoragePanelToAdd610Length = string.Empty;
            StoragePanelToAdd610Count = string.Empty;
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
