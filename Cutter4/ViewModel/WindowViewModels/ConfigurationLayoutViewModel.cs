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
        public bool CanApply { get; } = true; // TODO add check
        public Dictionary<PanelType, List<Configuration>> Result { get; private set; }

        IDictionary<PanelKey, int> storage;
        Dictionary<PanelType, List<Configuration>> originalConfs;
        
        IDialogService dialogService;
        ILocalizationService localizationService;

        public ConfigurationLayoutViewModel(IDialogService dialogService, ILocalizationService localizationService, Dictionary<PanelType, List<Configuration>> confs, IDictionary<PanelKey, int> storage)
        {
            this.dialogService = dialogService;
            this.localizationService = localizationService;

            ConfigurationGroups = [];
            foreach (KeyValuePair<PanelType, List<Configuration>> entry in confs)
            {
                ConfigurationGroupViewModel group = new(entry.Key);

                foreach (Configuration conf in entry.Value)
                    group.Configurations.Add(new ConfigurationViewModel(conf, group));

                ConfigurationGroups.Add(group);
            }

            this.storage = storage;
            originalConfs = confs;
            Result = [];
        }
        
        public async Task<bool> Apply()
        {
            // TODO add check

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
                    if (conf.Items.Count == 0) continue;

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

            Result = newConfs;
            return true;
        }
    }
}
