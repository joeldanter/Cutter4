using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cutter4.Domain;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Windows;

namespace Cutter4.ViewModel
{
    public partial class ConfigurationViewModel : ObservableValidator
    {
        [ObservableProperty]
        [Range(1, PanelKey.MaxLength)]
        [CapacityMustNotOverflow]
        int capacity;

        public ObservableCollection<ConfigurationItemViewModel> Items { get; }
        public bool CapacityOverflows { get; private set; }
        public Visibility DeleteVisibility => Items.Count == 1 ? Visibility.Visible : Visibility.Collapsed;

        public ConfigurationGroupViewModel Group { get; }
        const double ScaleMultiplier = 1.0 / 30.0;

        public ConfigurationViewModel(Configuration model, ConfigurationGroupViewModel group)
        {
            Group = group;
            capacity = model.Capacity;

            List<ConfigurationItemViewModel> newItems = [];
            foreach (KeyValuePair<Kapu, int> entry in model.ConfigurationItems)
            {
                for (int i = 0; i < entry.Value; i++)
                    newItems.Add(new ConfigurationItemViewModel(entry.Key.Length, entry.Key.Length * ScaleMultiplier));
            }

            Items = new(newItems.OrderByDescending(i => i.RealLength)) { new(0, 0, true) };
            Items.CollectionChanged += (_, _) => OnPropertyChanged(nameof(DeleteVisibility));

            Recalculate();
        }

        partial void OnCapacityChanged(int value) => Recalculate();

        void Recalculate()
        {
            RecalculateWaste();
            RecalculatePixelWidths();
        }

        void RecalculateWaste()
        {
            int waste = Capacity;
            for (int i = 0; i < Items.Count - 1; i++)
                waste -= Items[i].RealLength;

            CapacityOverflows = waste < 0;
            waste = Math.Max(waste, 0);

            ConfigurationItemViewModel wasteItem = Items[^1];
            wasteItem.RealLength = waste;
            wasteItem.ScaledLength = waste * ScaleMultiplier;

            ValidateProperty(Capacity, nameof(Capacity));
        }

        void RecalculatePixelWidths()
        {
            // solving the rounded sum problem
            double error = 0;

            foreach (ConfigurationItemViewModel item in Items)
            {
                double target = error + item.ScaledLength;
                int rounded = (int)Math.Round(target, MidpointRounding.AwayFromZero);
                error = target - rounded;
                item.PixelWidth = rounded;
            }
        }

        public void AddItem(int realLength)
        {
            int i = 0;
            while (i < Items.Count - 1 && Items[i].RealLength > realLength)
                i++;

            Items.Insert(i, new(realLength, realLength * ScaleMultiplier));

            Recalculate();
        }

        public void RemoveItem(ConfigurationItemViewModel item)
        {
            Items.Remove(item);

            Recalculate();
        }

        [RelayCommand]
        void Delete() => Group.Configurations.Remove(this);
    }

    public class CapacityMustNotOverflowAttribute : ValidationAttribute
    {
        public CapacityMustNotOverflowAttribute() {  }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (validationContext.ObjectInstance is ConfigurationViewModel vm && vm.CapacityOverflows)
            {
                return new ValidationResult(ErrorMessage, [validationContext.MemberName!]);
            }

            return ValidationResult.Success;
        }
    }
}
