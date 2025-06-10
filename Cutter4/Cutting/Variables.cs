using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cutter4.Cutting
{
    static class Colors
    {
        public static List<string> ColorNames { get; set; }
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
