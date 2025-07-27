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

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SPs500))]
        [NotifyPropertyChangedFor(nameof(SPs610))]
        PanelCollection<StoragePanel> sPsCollection;
        public List<StoragePanel> SPs500 { get => SPsCollection[new PanelType("500mm", SelectedStorageColor)]; }
        public List<StoragePanel> SPs610 { get => SPsCollection[new PanelType("610mm", SelectedStorageColor)]; }
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SPs500))]
        [NotifyPropertyChangedFor(nameof(SPs610))]
        PanelColors selectedStorageColor = PanelColors.AF500;
        [ObservableProperty]
        StoragePanel? selected500SP;
        [ObservableProperty]
        StoragePanel? selected610SP;
        List<Kapu> calculatedKapuk;
        PanelCollection<Configuration> calculatedConfs;

        [ObservableProperty]
        string new500SPLength = string.Empty;
        [ObservableProperty]
        string new500SPCount = string.Empty;
        public Action SelectNew500SPCount { get; set; }
        [ObservableProperty]
        string new610SPLength = string.Empty;
        [ObservableProperty]
        string new610SPCount = string.Empty;
        public Action SelectNew610SPCount { get; set; }

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
            storage.StoragePanelsChanged += (object sender, EventArgs e) => { SPsCollection = storage.StoragePanels(); };
            SPsCollection = storage.StoragePanels();

            Kapuk.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => { CanSave = false; CalculateCommand.NotifyCanExecuteChanged(); };
            Kapuk.ElementsPropertyChanged += (object sender, ElementPropertyChangedEventArgs e) => { CanSave = false; CalculateCommand.NotifyCanExecuteChanged(); };
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
        void DoubleClicked500SP()
        {
            New500SPLength = Selected500SP?.Length.ToString();
            Selected500SP = null;
            SelectNew500SPCount();
        }

        [RelayCommand]
        void DoubleClicked610SP()
        {
            New610SPLength = Selected610SP?.Length.ToString();
            Selected610SP = null;
            SelectNew610SPCount();
        }

        [RelayCommand]
        void Add500SP()
        {
            PanelType type = new PanelType("500mm", SelectedStorageColor);
            int length = Convert.ToInt32(New500SPLength);
            int count = Convert.ToInt32(New500SPCount);
            storage.ChangeStoragePanelCount(type, length, count);
            New500SPLength = string.Empty;
            New500SPCount = string.Empty;
        }

        [RelayCommand]
        void Remove500SP()
        {
            PanelType type = new PanelType("500mm", SelectedStorageColor);
            int length = Convert.ToInt32(New500SPLength);
            int count = Convert.ToInt32(New500SPCount);
            storage.ChangeStoragePanelCount(type, length, -count);
            New500SPLength = string.Empty;
            New500SPCount = string.Empty;
        }

        [RelayCommand]
        void Add610SP()
        {
            PanelType type = new PanelType("610mm", SelectedStorageColor);
            int length = Convert.ToInt32(New610SPLength);
            int count = Convert.ToInt32(New610SPCount);
            storage.ChangeStoragePanelCount(type, length, count);
            New610SPLength = string.Empty;
            New610SPCount = string.Empty;
        }

        [RelayCommand]
        void Remove610SP()
        {
            PanelType type = new PanelType("610mm", SelectedStorageColor);
            int length = Convert.ToInt32(New610SPLength);
            int count = Convert.ToInt32(New610SPCount);
            storage.ChangeStoragePanelCount(type, length, -count);
            New610SPLength = string.Empty;
            New610SPCount = string.Empty;
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
