using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cutter4.Domain;
using Cutter4.Persistence.Repositories;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cutter4.ViewModel
{
    public partial class ConfigurationLayoutViewModel : ObservableObject
    {
        public ObservableCollection<ConfigurationGroupViewModel> ConfigurationGroups { get; }
        [ObservableProperty]
        bool canApply;
        public Dictionary<PanelType, List<Configuration>> Result { get; private set; }

        IDictionary<PanelKey, int> storage;
        Dictionary<PanelType, List<Configuration>> originalConfs;
        
        IMessageDialogService dialogService;
        ILocalizationService localizationService;
         
        public ConfigurationLayoutViewModel(IMessageDialogService dialogService, ILocalizationService localizationService, Dictionary<PanelType, List<Configuration>> confs, IDictionary<PanelKey, int> storage)
        {
            this.dialogService = dialogService;
            this.localizationService = localizationService;

            ConfigurationGroups = [];
            foreach (KeyValuePair<PanelType, List<Configuration>> entry in confs)
            {
                PanelType type = entry.Key;
                List<int> lengthChoices = storage
                    .Where(x => entry.Key.Color == x.Key.Color && entry.Key.Width == x.Key.Width)
                    .Select(x => x.Key.Length)
                    .OrderDescending()
                    .ToList();

                ConfigurationGroupViewModel group = new(type, lengthChoices);
                group.Configurations.CollectionChanged += Configurations_CollectionChanged;

                foreach (Configuration conf in entry.Value)
                    group.Configurations.Add(new ConfigurationViewModel(conf, group));

                ConfigurationGroups.Add(group);
            }

            this.storage = storage;
            originalConfs = confs;
            Result = [];
        }

        private void Configurations_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (ConfigurationViewModel conf in e.OldItems)
                    conf.ErrorsChanged -= (_, _) => UpdateCanApply();

            if (e.NewItems != null)
                foreach (ConfigurationViewModel conf in e.NewItems)
                    conf.ErrorsChanged += (_, _) => UpdateCanApply();

            UpdateCanApply();
        }

        private void UpdateCanApply()
        {
            CanApply = ConfigurationGroups.All(g => g.Configurations.All(i => !i.HasErrors));
        }

        public async Task<bool> Apply()
        {
            Dictionary<PanelType, List<Configuration>> newConfs = [];

            foreach (ConfigurationGroupViewModel group in ConfigurationGroups)
            {
                PanelType type = group.Type;

                Dictionary<int, List<Kapu>> kapuk = originalConfs[type]
                    .SelectMany(x => x.ConfigurationItems
                        .SelectMany(y => Enumerable.Repeat(y.Key, y.Value)))
                    .GroupBy(x => x.Length)
                    .ToDictionary(g => g.Key, g => g.Reverse().ToList());

                foreach (ConfigurationViewModel conf in group.Configurations)
                {
                    if (conf.Items.Count == 1) continue;

                    Dictionary<Kapu, int> decoratedItems = [];

                    foreach (ConfigurationItemViewModel item in conf.Items)
                    {
                        if (item.IsWaste) break;

                        Kapu kapu = kapuk[item.RealLength].Last();
                        if (!decoratedItems.ContainsKey(kapu))
                            decoratedItems[kapu] = 0;
                        decoratedItems[kapu] += 1;

                        kapuk[item.RealLength].RemoveAt(kapuk[item.RealLength].Count - 1);
                    }

                    Configuration newConfiguration = new(new PanelKey(type.Color, type.Width, conf.Capacity), decoratedItems);
                    if (!newConfs.ContainsKey(type))
                        newConfs[type] = [];
                    newConfs[type].Add(newConfiguration);
                }
            }

            foreach (KeyValuePair<PanelKey, int> entry in Configuration.PanelCountChanges(newConfs.SelectMany(x => x.Value)))
            {
                PanelKey key = entry.Key;
                int delta = entry.Value;
                if (!storage.TryGetValue(key, out int available))
                    available = 0;

                if (available + delta < 0)
                {
                    dialogService.ShowError(localizationService.GetString("Error_View_CutReorder_InsufficientStock", key, available, -delta));
                    return false;
                }
            }

            Result = newConfs;
            return true;
        }
    }
}
