using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cutter4.Domain;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Cutter4.ViewModel
{
    public partial class ConfigurationGroupViewModel : ObservableObject
    {
        public PanelType Type { get; }
        public string DisplayName => $"{Type.Color} {Type.Width.GetDescription()}";

        public ObservableCollection<ConfigurationViewModel> Configurations { get; }
        
        public ConfigurationGroupViewModel(PanelType type)
        {
            Type = type;
            Configurations = [];
        }

        [RelayCommand]
        void AddEmptyConfiguration()
        {
            Configuration model = new(new PanelKey(Type.Color, Type.Width, PanelKey.MaxLength), []);
            Configurations.Add(new ConfigurationViewModel(model, this));
        }
    }
}
