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

        [ObservableProperty]
        string server = "127.0.0.1";
        [ObservableProperty]
        string databaseName = "cutter";
        [ObservableProperty]
        string userName = "main";
        [ObservableProperty]
        string password = "Almacitrom";
        [ObservableProperty]
        string pingResultText = string.Empty;
        [ObservableProperty]
        Brush pingResultColor = Brushes.Black;
        [ObservableProperty]
        DateTime? lastPingDate = null;

        public MainViewModel()
        {
            SaveDatabaseSettings();
            storage.StoragePanelsChanged += (object sender, EventArgs e) => { SPsCollection = storage.StoragePanels(); };
            SPsCollection = storage.StoragePanels();

            Kapuk.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => { CanSave = false; CalculateCommand.NotifyCanExecuteChanged(); };
            Kapuk.ElementsPropertyChanged += (object sender, ElementPropertyChangedEventArgs e) => { CanSave = false; CalculateCommand.NotifyCanExecuteChanged(); };

            storage.StorageEditsListChanged += (object sender, EventArgs e) => { LoadStorageHistory(); };
            storage.OrderListChanged += (object sender, EventArgs e) => { LoadStorageHistory(); };
            LoadStorageHistory();
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

        [RelayCommand]
        void PingDatabase()
        {
            if (storage.CanConnect())
            {
                PingResultText = "Connected!";
                PingResultColor = Brushes.Green;
            }
            else
            {
                PingResultText = "Failed to Connect";
                PingResultColor = Brushes.Red;
            }
            LastPingDate = DateTime.Now;
        }
    }
}
