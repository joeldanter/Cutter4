using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using Cutter4.Domain;
using Cutter4.Persistence.Repositories;

namespace Cutter4.ViewModel
{
    public partial class EditStorageViewModel : ObservableObject
    {
        IStorageRepository storageRepository;
        IMessageDialogService dialogService;
        ILocalizationService localizationService;
        ITruckDeliveryDialogService truckDeliveryDialogService;
        public ObservableCollection<DeltaCountEntry> DeltaCounts { get; } = [];
        
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddNewEditItemCommand))]
        PanelColors? newColor = null;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddNewEditItemCommand))]
        PanelWidths? newWidth = null;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddNewEditItemCommand))]
        string newLength = string.Empty;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddNewEditItemCommand))]
        string newDeltaCount = string.Empty;
        bool CanAddNewEditItem
        {
            get
            {
                return NewColor != null
                    && NewWidth != null
                    && int.TryParse(NewLength, out int newLength)
                    && 0 < newLength
                    && newLength <= PanelKey.MaxLength
                    && int.TryParse(NewDeltaCount, out int newDeltaCount)
                    && 0 != newDeltaCount;
            }
        }

        [ObservableProperty]
        string message = string.Empty;

        public bool CanApplyEdit { get { return DeltaCounts.Count > 0; } }

        public EditStorageViewModel(IStorageRepository storageRepository, IMessageDialogService dialogService, ILocalizationService localizationService, ITruckDeliveryDialogService truckDeliveryDialogService)
        {
            this.storageRepository = storageRepository;
            DeltaCounts.CollectionChanged += (sender, e) => { OnPropertyChanged(nameof(CanApplyEdit)); };

            this.dialogService = dialogService;
            this.localizationService = localizationService;
            this.truckDeliveryDialogService = truckDeliveryDialogService;
        }

        [RelayCommand(CanExecute = nameof(CanAddNewEditItem))]
        void AddNewEditItem()
        {
            if (NewColor == null || NewWidth == null)
                throw new UnreachableException();

            PanelKey panelKey = new ((PanelColors)NewColor, (PanelWidths)NewWidth, int.Parse(NewLength));
            int newDeltaCount = int.Parse(NewDeltaCount);
            
            int modifiedPanelIndex = DeltaCounts
                .ToList()
                .FindIndex(entry => entry.PanelKey == panelKey);

            if (modifiedPanelIndex == -1)
            {
                DeltaCounts.Add(new(panelKey, newDeltaCount));
            }
            else
            {
                DeltaCounts[modifiedPanelIndex] = new(panelKey, DeltaCounts[modifiedPanelIndex].DeltaCount + newDeltaCount);
                
                if (DeltaCounts[modifiedPanelIndex].DeltaCount == 0)
                {
                    DeltaCounts.RemoveAt(modifiedPanelIndex);
                }
            }

            NewColor = null;
            NewWidth = null;
            NewLength = string.Empty;
            NewDeltaCount = string.Empty;
        }

        public async Task<bool> Apply()
        {
            StorageEdit edit = new(DateTime.Now, Message, DeltaCounts.ToDictionary(x => x.PanelKey, x => x.DeltaCount));

            try
            {
                await storageRepository.EditStorageAsync(edit);
            }
            catch (UserFacingException e)
            {
                dialogService.ShowError(localizationService.GetString(e.ErrorKey, e.Arguments));
                return false;
            }
            return true;
        }

        [RelayCommand]
        public void TruckDelivery()
        {
            int? panelCounts = truckDeliveryDialogService.ShowDialog();
            if (panelCounts == null)
                return;

            DeltaCounts.Clear();
            foreach (PanelType type in PanelType.GetAllTypes())
            {
                PanelKey panelKey = new(type.Color, type.Width, PanelKey.MaxLength);
                DeltaCounts.Add(new(panelKey, (int)panelCounts));
            }
        }
    }

    public record DeltaCountEntry(PanelKey PanelKey, int DeltaCount);
}
