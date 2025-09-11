using MySqlX.XDevAPI.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

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
    
    public readonly record struct KapuSerialNumber
    {
        public string Value { get; }

        const int maxLength = 20;

        public KapuSerialNumber(string value)
        {
            if (!IsValid(value, out string errorMessage))
                throw new ArgumentException(errorMessage);
            Value = value;
        }

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

        public override string ToString() => Value;
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

        public static PanelType[] GetAllTypes()
        {
            List<PanelType> result = [];
            foreach (PanelColors color in Enum.GetValues(typeof(PanelColors)))
            {
                result.Add(new PanelType("500mm", color));
                result.Add(new PanelType("610mm", color));
            }
            return [.. result];
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
        public KapuSerialNumber SerialNumber { get; }

        public KapuPanel(int length, KapuSerialNumber serialNumber)
        {
            if (length < 1)
                throw new ArgumentException("The length of a panel can not be smaller than 1");
            Length = length;
            SerialNumber = serialNumber;
        }
    }
}
