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
        public Storage Storage { get; } = new();

        [ObservableProperty]
        int selectedTabIndex;

        public FullyObservableCollection<KapuViewModel> Kapuk { get; } = new FullyObservableCollection<KapuViewModel>();
        [ObservableProperty]
        string outputText = string.Empty;
        bool canCalculate { get => Kapuk.Count != 0 && Kapuk.All(kapu => !kapu.HasErrors); }
        [ObservableProperty]
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
        [NotifyPropertyChangedFor(nameof(CanRevertOrder))]
        ObservableCollection<IStorageHistoryEntry> storageHistory = new ObservableCollection<IStorageHistoryEntry>();
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanRevertOrder))]
        IStorageHistoryEntry selectedStorageHistoryEntry = null;
        public bool CanRevertOrder 
        {
            get 
            {
                return SelectedStorageHistoryEntry is not null && SelectedStorageHistoryEntry == LastOrderSaveStorageHistoryEntry;
            }
        }
        IStorageHistoryEntry LastOrderSaveStorageHistoryEntry => StorageHistory.Where(entry => entry is OrderSaveStorageHistoryEntry).MaxBy(entry => entry.DateTime.Ticks);

        public MainViewModel()
        {
            Kapuk.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => { CanSave = false; CalculateCommand.NotifyCanExecuteChanged(); };
            Kapuk.ElementsPropertyChanged += (object sender, ElementPropertyChangedEventArgs e) => { CanSave = false; CalculateCommand.NotifyCanExecuteChanged(); };

            Storage.StoragePanelsChanged += (object sender, EventArgs e) => { LoadStoragePanels(); };
            Storage.StorageEditsListChanged += (object sender, EventArgs e) => { LoadStorageHistory(); };
            Storage.OrderListChanged += (object sender, EventArgs e) => { LoadStorageHistory(); };
        }

        public void LoadDatabase()
        {
            Storage.LoadSettings();

            LoadStoragePanels();
            LoadStorageHistory();
        }

        void LoadStoragePanels()
        {
            StoragePanelCounts = Storage.StoragePanels();
        }

        void LoadStorageHistory()
        {
            List<IStorageHistoryEntry> history = new List<IStorageHistoryEntry>();
            foreach (Order order in Storage.ListPreviousOrders())
            {
                string orderOutput = outputGenerator.GenerateOutput(order.Kapuk, order.Configurations);
                OrderSaveStorageHistoryEntry orderEntry = new OrderSaveStorageHistoryEntry(order, orderOutput);
                history.Add(orderEntry);
            }
            foreach (StorageEdit edit in Storage.ListPreviousStorageEdits())
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
            calculatedConfs = solver.Solve(panelCollectionToCut, Storage.StoragePanels());
            OutputText = outputGenerator.GenerateOutput(calculatedKapuk, calculatedConfs);
            CanSave = true;
        }

        public void Save()
        {
            Storage.SaveNewCut(calculatedKapuk, calculatedConfs);
            CanSave = false;
        }

        [RelayCommand]
        void NewCut()
        {
            Kapuk.Clear();
            OutputText = string.Empty;
        }

        public void RevertOrder()
        {
            ReadOnlyCollection<Kapu> orderKapuk = ((OrderSaveStorageHistoryEntry)LastOrderSaveStorageHistoryEntry).Kapuk;
            Storage.UndoLastSavedCut();
            SelectedTabIndex = 0;
            Kapuk.Clear();
            foreach (Kapu kapu in orderKapuk)
            {
                Kapuk.Add(new KapuViewModel(kapu));
            }
            OutputText = string.Empty;
        }
    }
}
