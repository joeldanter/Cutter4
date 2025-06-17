using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cutter4.Cutting
{
    public enum PanelColors
    {
        AF500,
        F100,
        F201,
        F301,
        F401,
        F9006,
        G101,
        G103
    }
    
    public struct PanelType
    {
        public string Width { get; }
        public PanelColors Color { get; }

        public PanelType(string width, PanelColors color)
        {
            if (width != "500mm" && width != "610mm")
            {
                throw new ArgumentException("Invalid width value was given: " + width);
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
