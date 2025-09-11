using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cutter4.Model.Cutting;
using Cutter4.Model.DataBase;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Cutter4.ViewModel
{
    public partial class MainViewModel : ObservableObject
    {
        ICuttingStockSolver solver = new GreedySolver();
        ICutOutputGenerator outputGenerator = new CompactOutputGenerator();
        Storage storage = new Storage();

        [ObservableProperty]
        int selectedTabIndex;

        public FullyObservableCollection<KapuViewModel> Kapuk { get; } = new FullyObservableCollection<KapuViewModel>();
        [ObservableProperty]
        string outputText = string.Empty;
        bool canCalculate { get => Kapuk.Count != 0 && Kapuk.All(kapu => !kapu.HasErrors); }
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        bool canSave = false;
        List<Kapu> calculatedKapuk;
        PanelCollection<Configuration> calculatedConfs;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MaxLength500SPCount))]
        [NotifyPropertyChangedFor(nameof(MaxLength610SPCount))]
        [NotifyPropertyChangedFor(nameof(SPs500))]
        [NotifyPropertyChangedFor(nameof(SPs610))]
        Dictionary<Panel, int> storagePanelCounts = [];
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MaxLength500SPCount))]
        [NotifyPropertyChangedFor(nameof(MaxLength610SPCount))]
        [NotifyPropertyChangedFor(nameof(SPs500))]
        [NotifyPropertyChangedFor(nameof(SPs610))]
        PanelColors selectedStorageColor = PanelColors.AF500;
        public int MaxLength500SPCount
        {
            get
            {
                foreach (KeyValuePair<Panel, int> entry in StoragePanelCounts)
                {
                    if (entry.Key.Type == new PanelType("500mm", SelectedStorageColor)
                        && entry.Key.Length == Kapu.MaxLength)
                    {
                        return entry.Value;
                    }
                }
                return 0;
            }
        }
        public int MaxLength610SPCount
        {
            get
            {
                foreach(KeyValuePair<Panel, int> entry in StoragePanelCounts)
                {
                    if (entry.Key.Type == new PanelType("610mm", SelectedStorageColor)
                        && entry.Key.Length == Kapu.MaxLength)
                    {
                        return entry.Value;
                    }
                }
                return 0;
            }
        }
        public List<int> SPs500
        {
            get => [.. StoragePanelCounts
                .Where(entry => entry.Key.Type == new PanelType("500mm", SelectedStorageColor))
                .Where(entry => entry.Key.Length != Kapu.MaxLength)
                .SelectMany(entry => Enumerable.Repeat(entry.Key.Length, entry.Value)) ];
        }
        public List<int> SPs610
        {
            get => [.. StoragePanelCounts
                .Where(entry => entry.Key.Type == new PanelType("610mm", SelectedStorageColor))
                .Where(entry => entry.Key.Length != Kapu.MaxLength)
                .SelectMany(entry => Enumerable.Repeat(entry.Key.Length, entry.Value)) ];
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RevertOrderCommand))]
        ObservableCollection<IStorageHistoryEntry> storageHistory = new ObservableCollection<IStorageHistoryEntry>();
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RevertOrderCommand))]
        IStorageHistoryEntry selectedStorageHistoryEntry = null;
        bool canRevertOrder 
        {
            get 
            {
                return SelectedStorageHistoryEntry == lastOrderSaveStorageHistoryEntry;
            }
        }
        IStorageHistoryEntry lastOrderSaveStorageHistoryEntry => StorageHistory.MaxBy(entry => entry.DateTime.Ticks * (entry is OrderSaveStorageHistoryEntry ? 1 : 0));

        const string dataBaseSettingsPath = "dbsettings.json";

        public MainViewModel()
        {
            Kapuk.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => { CanSave = false; CalculateCommand.NotifyCanExecuteChanged(); };
            Kapuk.ElementsPropertyChanged += (object sender, ElementPropertyChangedEventArgs e) => { CanSave = false; CalculateCommand.NotifyCanExecuteChanged(); };

            storage.StoragePanelsChanged += (object sender, EventArgs e) => { LoadStoragePanels(); };
            storage.StorageEditsListChanged += (object sender, EventArgs e) => { LoadStorageHistory(); };
            storage.OrderListChanged += (object sender, EventArgs e) => { LoadStorageHistory(); };
        }

        public bool TryLoadDatabase(out string errorMessage)
        {
            if (!File.Exists(dataBaseSettingsPath))
            {
                errorMessage = $"Can't find the database settings file at {Path.GetFullPath(dataBaseSettingsPath)}";
                return false;
            }

            string jsonStr = File.ReadAllText(dataBaseSettingsPath);
            DataBaseSettings settings;
            try
            {
                settings = (DataBaseSettings)JsonSerializer.Deserialize(jsonStr, typeof(DataBaseSettings));
            }
            catch (JsonException e)
            {
                errorMessage = $"Failed to deserialize database settings file:\n{e.Message}";
                return false;
            }
            
            if (settings.IsAnyPropertyNull())
            {
                errorMessage = $"Failed to load all details from database settings file";
                return false;
            }

            storage.Server = settings.Server;
            storage.DatabaseName = settings.DatabaseName;
            storage.UserName = settings.UserName;
            storage.Password = settings.Password;

            if (!storage.CanConnect())
            {
                errorMessage = "Failed to connect to the database using the provided credentials";
                return false;
            }

            LoadStoragePanels();
            LoadStorageHistory();
            errorMessage = "";
            return true;
        }

        void LoadStoragePanels()
        {
            StoragePanelCounts = storage.StoragePanels();
        }

        void LoadStorageHistory()
        {
            List<IStorageHistoryEntry> history = new List<IStorageHistoryEntry>();
            foreach (Order order in storage.ListPreviousOrders())
            {
                string orderOutput = outputGenerator.GenerateOutput(order.Kapuk, order.Configurations);
                OrderSaveStorageHistoryEntry orderEntry = new OrderSaveStorageHistoryEntry(order, orderOutput);
                history.Add(orderEntry);
            }
            foreach (StorageEdit edit in storage.ListPreviousStorageEdits())
            {
                EditStorageHistoryEntry editEntry = new EditStorageHistoryEntry(edit);
                history.Add(editEntry);
            }
            history.Sort((entry1, entry2) => entry2.DateTime.CompareTo(entry1.DateTime));

            StorageHistory = new ObservableCollection<IStorageHistoryEntry>(history);
        }

        [RelayCommand(CanExecute = nameof(canCalculate))]
        void Calculate()
        {
            calculatedKapuk = Kapuk.Select(kapu => kapu.ActualKapu()).ToList();
            PanelCollection<KapuPanel> panelCollectionToCut = Kapu.ProcessPanels(calculatedKapuk);
            calculatedConfs = solver.Solve(panelCollectionToCut, storage.StoragePanels());
            OutputText = outputGenerator.GenerateOutput(calculatedKapuk, calculatedConfs);
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

        public void EditStorage(string message, Dictionary<Panel, int> deltaCounts)
        {
            storage.EditStoragePanels(message, deltaCounts);
        }

        [RelayCommand(CanExecute = nameof(canRevertOrder))]
        void RevertOrder()
        {
            storage.UndoLastSavedCut();
            SelectedTabIndex = 0;
            ReadOnlyCollection<Kapu> orderKapuk = ((OrderSaveStorageHistoryEntry)lastOrderSaveStorageHistoryEntry).Kapuk;
            Kapuk.Clear();
            foreach (Kapu kapu in orderKapuk)
            {
                Kapuk.Add(new KapuViewModel(kapu));
            }
            OutputText = string.Empty;
        }
    }
}
