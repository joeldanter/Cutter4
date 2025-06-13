using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cutter4.Cutting
{
    static class PanelColors
    {
        public static ReadOnlyCollection<string> ColorNames { get; private set; }

        public static void LoadColorNames(List<string> newColorNames)
        {
            ColorNames = new ReadOnlyCollection<string>(newColorNames);
        }
    }
    
    public struct PanelType
    {
        public readonly string Width;
        public readonly string Color;

        public PanelType(string width, string color)
        {
            Width = width;
            Color = color;
        }
    }

    public struct Panel
    {
        public readonly int Length;
        public readonly string SerialNumber;

        public Panel(int length, string serialNumber)
        {
            Length = length;
            SerialNumber = serialNumber;
        }
    }
}
