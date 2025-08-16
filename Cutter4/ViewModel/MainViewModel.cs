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
        List<Kapu> calculatedKapuk;
        PanelCollection<Configuration> calculatedConfs;

        [ObservableProperty]
        ObservableCollection<IStorageAction> storageActions = new ObservableCollection<IStorageAction>();
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RevertOrderCommand))]
        IStorageAction selectedStorageAction = null;
        bool canRevertOrder 
        {
            get 
            {
                return SelectedStorageAction == lastOrderSaveAction;
            }
        }
        IStorageAction lastOrderSaveAction => StorageActions.MaxBy(action => action.DateTime.Ticks * (action is OrderSaveAction ? 1 : 0));

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

            storage.StorageEditsListChanged += (object sender, EventArgs e) => { LoadStorageActions(); };
            storage.OrderListChanged += (object sender, EventArgs e) => { LoadStorageActions(); };
            LoadStorageActions();
        }

        void LoadStorageActions()
        {
            List<IStorageAction> actions = new List<IStorageAction>();
            foreach (Order order in storage.ListPreviousOrders())
            {
                string orderOutput = outputGenerator.GenerateOutput(order.Kapuk, order.Configurations);
                OrderSaveAction orderAction = new OrderSaveAction(order, orderOutput);
                actions.Add(orderAction);
            }
            foreach (StorageEdit edit in storage.ListPreviousStorageEdits())
            {
                StorageEditAction editAction = new StorageEditAction(edit);
                actions.Add(editAction);
            }
            actions.Sort((action1, action2) => action2.DateTime.CompareTo(action1.DateTime));

            StorageActions = new ObservableCollection<IStorageAction>(actions);
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
            ReadOnlyCollection<Kapu> orderKapuk = ((OrderSaveAction)lastOrderSaveAction).Kapuk;
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
