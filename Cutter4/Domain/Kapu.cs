using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Cutter4.Domain
{    
    public class Kapu
    {
        int length;
        public int Length
        {
            get
            {
                return length;
            }
            init
            {
                if (1 <= value && value <= PanelKey.MaxLength) length = value;
                else throw new ArgumentException($"The length must be between 1 and {PanelKey.MaxLength}");
            }
        }

        int height;
        public int Height
        {
            get
            {
                return height;
            }
            init
            {
                if (1 <= value && value <= MaxHeight) height = value;
                else throw new ArgumentException($"The height must be between 1 and {MaxHeight}");
            }
        }

        public string SerialNumber { get; init; }

        public PanelColors Color { get; init; }

        public const int MaxHeight = 5210;

        static readonly int[,] KapuHeights =
        {
            { 550, 1, 0 },
            { 660, 0, 1 },
            { 1050, 2, 0 },
            { 1270, 0, 2 },
            { 1550, 3, 0 },
            { 1880, 0, 3 },
            { 2050, 4, 0 },
            { 2160, 3, 1 },
            { 2270, 2, 2 },
            { 2380, 1, 3 },
            { 2490, 0, 4 },
            { 2550, 5, 0 },
            { 2660, 4, 1 },
            { 2770, 3, 2 },
            { 2880, 2, 3 },
            { 2990, 1, 4 },
            { 3100, 0, 5 },
            { 3270, 4, 2 },
            { 3380, 3, 3 },
            { 3490, 2, 4 },
            { 3600, 1, 5 },
            { 3710, 0, 6 },
            { 3880, 4, 3 },
            { 3990, 3, 4 },
            { 4100, 2, 5 },
            { 4210, 1, 6 },
            { 4320, 0, 7 },
            { 4490, 4, 4 },
            { 4600, 3, 5 },
            { 4710, 2, 6 },
            { 4820, 1, 7 },
            { 4930, 0, 8 },
            { 5100, 4, 5 },
            { 5210, 3, 6 }
        };

        public Kapu(int length, int height, PanelColors color, string serialNumber)
        {
            Length = length;
            Height = height;
            Color = color;
            SerialNumber = serialNumber;
        }

        public void GetPanelWidths(out int amount500mm, out int amount610mm)
        {
            for (int i = 0; i < KapuHeights.GetLength(0); i++)
            {
                if (Height <= KapuHeights[i, 0])
                {
                    amount500mm = KapuHeights[i, 1];
                    amount610mm = KapuHeights[i, 2];
                    return;
                }
            }

            throw new UnreachableException();
        }

        public static Dictionary<PanelType, List<Kapu>> ProcessPanels(IEnumerable<Kapu> kapuk)
        {
            Dictionary<PanelType, List<Kapu>> result = [];

            foreach (Kapu kapu in kapuk)
            {
                kapu.GetPanelWidths(out int amount500mm, out int amount610mm);

                if (amount500mm > 0)
                {
                    PanelType type = new(kapu.Color, PanelWidths.Width500mm);
                    if (!result.ContainsKey(type))
                        result[type] = [];
                    result[type].AddRange(Enumerable.Repeat(kapu, amount500mm));
                }

                if (amount610mm > 0)
                {
                    PanelType type = new(kapu.Color, PanelWidths.Width610mm);
                    if (!result.ContainsKey(type))
                        result[type] = [];
                    result[type].AddRange(Enumerable.Repeat(kapu, amount610mm));
                }
            }

            return result;
        }
    }

    public static class KapuSerialNumber
    {
        const int maxLength = 20;

        public static bool IsValid(string serialNumber, out string errorMessage)
        {
            if (serialNumber == null)
            {
                errorMessage = "The given serial number was null";
                return false;
            }
            if (serialNumber.Length == 0)
            {
                errorMessage = "The serial number can not be empty";
                return false;
            }
            if (char.IsWhiteSpace(serialNumber[0]) || char.IsWhiteSpace(serialNumber[^1]))
            {
                errorMessage = "The serial number must not begin or end with whitespace";
                return false;
            }
            if (maxLength < serialNumber.Length)
            {
                errorMessage = $"The serial number must be no more than {maxLength} characters";
                return false;
            }

            errorMessage = "";
            return true;
        }
    }
}
