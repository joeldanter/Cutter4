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
        public ObservableCollection<DeltaCountEntry> AddedCounts { get; } = [];
        public ObservableCollection<DeltaCountEntry> RemovedCounts { get; } = [];

        [ObservableProperty]
        string message = string.Empty;

        public bool CanApplyEdit
        {
            get => AddedCounts.All(x => !x.HasErrors)
                && RemovedCounts.All(x => !x.HasErrors);
        }

        public EditStorageViewModel(IStorageRepository storageRepository, IMessageDialogService dialogService, ILocalizationService localizationService, ITruckDeliveryDialogService truckDeliveryDialogService)
        {
            this.storageRepository = storageRepository;
            this.dialogService = dialogService;
            this.localizationService = localizationService;
            this.truckDeliveryDialogService = truckDeliveryDialogService;

            AddedCounts.CollectionChanged += AddedCounts_CollectionChanged;
            RemovedCounts.CollectionChanged += AddedCounts_CollectionChanged;
        }

        void AddedCounts_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (DeltaCountEntry item in e.OldItems)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                    item.ErrorsChanged -= Item_ErrorsChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (DeltaCountEntry item in e.NewItems)
                {
                    item.PropertyChanged += Item_PropertyChanged;
                    item.ErrorsChanged += Item_ErrorsChanged;
                }
            }

            MarkDuplicates();
            OnPropertyChanged(nameof(CanApplyEdit));
        }

        void Item_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DeltaCountEntry.Color)
             || e.PropertyName == nameof(DeltaCountEntry.Width)
             || e.PropertyName == nameof(DeltaCountEntry.Length))
                MarkDuplicates();
        }

        private void Item_ErrorsChanged(object? sender, System.ComponentModel.DataErrorsChangedEventArgs e)
        {
            OnPropertyChanged(nameof(CanApplyEdit));
        }

        void MarkDuplicates()
        {
            List<DeltaCountEntry> all = AddedCounts.Concat(RemovedCounts).ToList();
            
            HashSet<(PanelColors?, PanelWidths?, int)> collisions = all
                .Where(e => !e.GetErrors(nameof(DeltaCountEntry.Color)).Any())
                .Where(e => !e.GetErrors(nameof(DeltaCountEntry.Width)).Any())
                .Where(e => !e.GetErrors(nameof(DeltaCountEntry.Length)).Any())
                .GroupBy(e => (e.Color, e.Width, e.Length))
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToHashSet();

            foreach (DeltaCountEntry entry in all)
                entry.IsDuplicate = collisions.Contains((entry.Color, entry.Width, entry.Length));
        }

        public async Task<bool> Apply()
        {
            Dictionary<PanelKey, int> editItems = [];
            foreach (DeltaCountEntry entry in AddedCounts)
                editItems[new PanelKey((PanelColors)entry.Color, (PanelWidths)entry.Width, entry.Length)] = entry.Count;
            foreach (DeltaCountEntry entry in RemovedCounts)
                editItems[new PanelKey((PanelColors)entry.Color, (PanelWidths)entry.Width, entry.Length)] = -entry.Count;

            StorageEdit edit = new(DateTime.Now, Message, editItems);

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

            AddedCounts.Clear();
            RemovedCounts.Clear();
            foreach (PanelType type in PanelType.GetAllTypes())
            {
                PanelKey panelKey = new(type.Color, type.Width, PanelKey.MaxLength);
                AddedCounts.Add(new(panelKey.Color, panelKey.Width, panelKey.Length, (int)panelCounts));
            }
        }
    }

    public partial class DeltaCountEntry : ObservableValidator
    {
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required]
        PanelColors? color;
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required]
        PanelWidths? width;
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(1, PanelKey.MaxLength)]
        int length;
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(1, int.MaxValue)]
        int count;
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [MustBeFalse]
        bool isDuplicate;

        public DeltaCountEntry()
        {
            Color = null;
            Width = null;
            Length = 0;
            count = 0;
            IsDuplicate = false;
            
            ValidateAllProperties();
        }

        public DeltaCountEntry(PanelColors color, PanelWidths width, int length, int count)
        {
            Color = color;
            Width = width;
            Length = length;
            Count = count;
            IsDuplicate = false;

            ValidateAllProperties();
        }
    }

    public class MustBeFalseAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            return value is bool b && !b;
        }
    }
}
