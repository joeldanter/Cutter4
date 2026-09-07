using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Media;

namespace Cutter4.ViewModel
{
    public partial class ConfigurationItemViewModel : ObservableObject
    {
        [ObservableProperty]
        int realLength;
        [ObservableProperty]
        double scaledLength;
        [ObservableProperty]
        int pixelWidth;

        public bool IsWaste { get; }
        public Brush Background { get; }

        public ConfigurationItemViewModel(int realLength, double scaledLength, bool isWaste = false)
        {
            RealLength = realLength;
            ScaledLength = scaledLength;
            PixelWidth = (int)Math.Round(scaledLength, MidpointRounding.AwayFromZero); // it will probably be overriden later
            IsWaste = isWaste;
            Background = isWaste ? Brushes.LightGray : Brushes.White;
        }
    }
}
