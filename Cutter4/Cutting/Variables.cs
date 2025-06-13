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
        public string Width { get; }
        public string Color { get; }

        public PanelType(string width, string color)
        {
            if (width != "500mm" && width != "610mm")
            {
                throw new ArgumentException("Invalid width value was given: " + width);
            }
            if (!PanelColors.ColorNames.Contains(color))
            {
                throw new ArgumentException("Invalid color value was given: " + color);
            }

            Width = width;
            Color = color;
        }
    }

    public struct Panel
    {
        public int Length { get; }
        public string SerialNumber { get; }

        public Panel(int length, string serialNumber)
        {
            Length = length;
            SerialNumber = serialNumber;
        }
    }
}
