using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cutter4.Model.Cutting
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
    
    public readonly record struct PanelType
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

    public readonly record struct Panel
    {
        public PanelType Type { get; }
        public int Length { get; }

        public Panel(PanelType type, int length)
        {
            if (length < 1)
                throw new ArgumentException("The length of a panel can not be smaller than 1");
            Type = type;
            Length = length;
        }
    }

    public readonly record struct KapuPanel
    {
        public int Length { get; }
        public string SerialNumber { get; }

        public KapuPanel(int length, string serialNumber)
        {
            if (length < 1)
                throw new ArgumentException("The length of a panel can not be smaller than 1");
            Length = length;
            SerialNumber = serialNumber;
        }
    }
}
