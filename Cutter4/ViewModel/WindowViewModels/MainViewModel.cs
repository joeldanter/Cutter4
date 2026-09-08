using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Cutter4.Domain;
using Cutter4.Domain.Cutting;
using Cutter4.Persistence;
using Cutter4.Persistence.Repositories;

namespace Cutter4.ViewModel
{
    public partial class MainViewModel : ObservableObject
    {
        CutManager cutManager = new CutManager(new LPSolver());
        IOrderRepository orderRepository;
        IStorageRepository storageRepository;
        ICutOutputGenerator outputGenerator = new CompactOutputGenerator();
        IMessageDialogService messageDialogService;
        ILocalizationService localizationService;
        IConfigurationLayoutDialogService configurationLayoutDialogService;
        IEditStorageDialogService editStorageDialogService;

        [ObservableProperty]
        int selectedTabIndex;

        public FullyObservableCollection<KapuViewModel> Kapuk { get; } = [];
        bool canCalculate { get => Kapuk.Count != 0 && Kapuk.All(kapu => !kapu.HasErrors); }
        [ObservableProperty]
        string outputText = string.Empty;
        [ObservableProperty]
        bool canSave = false;
        List<Kapu> calculatedKapuk = [];
        public Dictionary<PanelType, List<Configuration>> CalculatedConfs { get; private set; } = [];

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MaxLength500SPCount))]
        [NotifyPropertyChangedFor(nameof(MaxLength610SPCount))]
        [NotifyPropertyChangedFor(nameof(SPs500))]
        [NotifyPropertyChangedFor(nameof(SPs610))]
        IDictionary<PanelKey, int> storage = new Dictionary<PanelKey, int>();
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
                PanelKey key = new(SelectedStorageColor, PanelWidths.Width500mm, PanelKey.MaxLength);

                return Storage.TryGetValue(key, out var value) ? value : 0;
            }
        }
        public int MaxLength610SPCount
        {
            get
            {
                PanelKey key = new(SelectedStorageColor, PanelWidths.Width610mm, PanelKey.MaxLength);

                return Storage.TryGetValue(key, out var value) ? value : 0;
            }
        }
        public List<int> SPs500
        {
            get => [.. Storage
                .Where(entry => entry.Key.Color == SelectedStorageColor
                    && entry.Key.Width == PanelWidths.Width500mm
                    && entry.Key.Length != PanelKey.MaxLength)
                .OrderByDescending(entry => entry.Key.Length)
                .SelectMany(entry => Enumerable.Repeat(entry.Key.Length, entry.Value)) ];
        }
        public List<int> SPs610
        {
            get => [.. Storage
                .Where(entry => entry.Key.Color == SelectedStorageColor
                    && entry.Key.Width == PanelWidths.Width610mm
                    && entry.Key.Length != PanelKey.MaxLength)
                .OrderByDescending(entry => entry.Key.Length)
                .SelectMany(entry => Enumerable.Repeat(entry.Key.Length, entry.Value)) ];
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanRevertOrder))]
        ObservableCollection<IStorageHistoryEntry> storageHistory = new ObservableCollection<IStorageHistoryEntry>();
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanRevertOrder))]
        IStorageHistoryEntry? selectedStorageHistoryEntry = null;
        public bool CanRevertOrder 
        {
            get 
            {
                return SelectedStorageHistoryEntry is not null && SelectedStorageHistoryEntry == LastOrderSaveStorageHistoryEntry;
            }
        }
        IStorageHistoryEntry? LastOrderSaveStorageHistoryEntry => StorageHistory.Where(entry => entry is OrderSaveStorageHistoryEntry).MaxBy(entry => entry.DateTime.Ticks);

        public MainViewModel(IOrderRepository orderRepository, IStorageRepository storageRepository, IMessenger messenger, IMessageDialogService messageDialogService, ILocalizationService localizationService, IConfigurationLayoutDialogService configurationLayoutDialogService, IEditStorageDialogService editStorageDialogService)
        {
            Kapuk.CollectionChanged += (sender, e) => { CanSave = false; CalculateCommand.NotifyCanExecuteChanged(); };
            Kapuk.ElementsPropertyChanged += (sender, e) => { CanSave = false; CalculateCommand.NotifyCanExecuteChanged(); };

            messenger.Register<StorageChangedMessage>(this, (r, m) => LoadStoragePanels());
            messenger.Register<EditsChangedMessage>(this, (r, m) => LoadStorageHistory());
            messenger.Register<OrdersChangedMessage>(this, (r, m) => LoadStorageHistory());
            this.orderRepository = orderRepository;
            this.storageRepository = storageRepository;
            this.messageDialogService = messageDialogService;
            this.localizationService = localizationService;
            this.configurationLayoutDialogService = configurationLayoutDialogService;
            this.editStorageDialogService = editStorageDialogService;

            Kapuk.Add(new KapuViewModel() { Length = 2100, Height = 3000, Color = PanelColors.F100, SerialNumber = "aaa" }); // TODO remove
            Kapuk.Add(new KapuViewModel() { Length = 2300, Height = 4000, Color = PanelColors.F100, SerialNumber = "bbb" });
        }
        
        public async Task LoadDatabase()
        {
            await LoadStoragePanels();
            await LoadStorageHistory();
        }
        
        async Task LoadStoragePanels()
        {
            Storage = await storageRepository.StoragePanelsAsync();
        }
 
        async Task LoadStorageHistory()
        {
            List<IStorageHistoryEntry> history = [];
            foreach (Order order in await orderRepository.ListOrdersAsync())
            {
                Dictionary<PanelType, List<Configuration>> confs = order.Configurations
                    .GroupBy(c => new PanelType(c.PanelKey.Color, c.PanelKey.Width))
                    .ToDictionary(g => g.Key, g => g.ToList());

                string orderOutput = outputGenerator.GenerateOutput(order.Kapuk, confs, order.Date);
                OrderSaveStorageHistoryEntry orderEntry = new(order, orderOutput);
                history.Add(orderEntry);
            }
            foreach (StorageEdit edit in await storageRepository.ListStorageEditsAsync())
            {
                EditStorageHistoryEntry editEntry = new EditStorageHistoryEntry(edit);
                history.Add(editEntry);
            }
            history.Sort((entry1, entry2) => entry2.DateTime.CompareTo(entry1.DateTime));

            StorageHistory = new ObservableCollection<IStorageHistoryEntry>(history);
        }

        [RelayCommand(CanExecute = nameof(canCalculate))]
        async Task Calculate()
        {
            calculatedKapuk = Kapuk.Select(kapu => kapu.ActualKapu()).ToList();
            Dictionary<PanelType, List<Kapu>> panelsToCut = Kapu.ProcessPanels(calculatedKapuk);

            try
            {
                CalculatedConfs = cutManager.Solve(panelsToCut, Storage)
                    .Select(x => new KeyValuePair<PanelType, List<Configuration>>(
                        x.Key,
                        x.Value.OrderBy(p => p.Waste).ToList()))
                    .OrderBy(x => x.Key)
                    .ToDictionary();
            }
            catch (UserFacingException e)
            {
                messageDialogService.ShowError(localizationService.GetString(e.ErrorKey, e.Arguments));
                return;
            }
            OutputText = outputGenerator.GenerateOutput(calculatedKapuk, CalculatedConfs, DateTime.Now);
            CanSave = true;
        }

        [RelayCommand]
        public async Task Save()
        {
            Order newOrder = new()
            {
                Date = DateTime.Now,
                Kapuk = calculatedKapuk,
                Configurations = [.. CalculatedConfs.SelectMany(x => x.Value)]
            };
            try
            {
                await orderRepository.SaveNewOrderAsync(newOrder);
            }
            catch (UserFacingException e)
            {
                messageDialogService.ShowError(localizationService.GetString(e.ErrorKey, e.Arguments));
                return;
            }
            CanSave = false;
        }
        
        [RelayCommand]
        void NewCut()
        {
            Kapuk.Clear();
            OutputText = string.Empty;
        }

        [RelayCommand]
        public void ReorderCut()
        {
            Dictionary<PanelType, List<Configuration>>? result = configurationLayoutDialogService.ShowDialog(CalculatedConfs, Storage);
            if (result == null)
                return;

            CalculatedConfs = result;
            OutputText = outputGenerator.GenerateOutput(calculatedKapuk, CalculatedConfs, DateTime.Now);
        }

        [RelayCommand]
        public void EditStorage()
        {
            editStorageDialogService.ShowDialog(Storage);
        }

        [RelayCommand]
        public async Task RevertOrder()
        {
            if (LastOrderSaveStorageHistoryEntry is not OrderSaveStorageHistoryEntry)
                return;

            ReadOnlyCollection<Kapu> orderKapuk = ((OrderSaveStorageHistoryEntry)LastOrderSaveStorageHistoryEntry).Kapuk;

            try 
            { 
                await orderRepository.UndoLastOrderAsync();
            }
            catch (UserFacingException e)
            {
                messageDialogService.ShowError(localizationService.GetString(e.ErrorKey, e.Arguments));
                return;
            }

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
