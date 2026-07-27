using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Cutter4.Domain
{
    public enum PanelColors
    {
        AF500 = 0,
        F100,
        F201,
        F301,
        F401,
        F9006,
        G101,
        G103
    }
    
    public enum PanelWidths
    {
        [Description("500mm")]
        Width500mm,
        [Description("610mm")]
        Width610mm
    }

    public static class EnumExtensions
    {
        public static string GetDescription(this Enum value)
        {
            FieldInfo? field = value.GetType().GetField(value.ToString());
            DescriptionAttribute? attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? value.ToString();
        }
    }

    public readonly record struct PanelType
    {
        public PanelColors Color { get; }
        public PanelWidths Width { get; }

        public PanelType(PanelColors color, PanelWidths width)
        {
            Color = color;
            Width = width;
        }

        public static PanelType[] GetAllTypes()
        {
            List<PanelType> result = [];
            foreach (PanelColors color in Enum.GetValues(typeof(PanelColors)))
            {
                foreach (PanelWidths width in Enum.GetValues(typeof(PanelWidths)))
                {
                    result.Add(new PanelType(color, width));
                }
            }
            return [.. result];
        }

        public override string ToString()
        {
            return $"{Color.GetDescription()} {Width.GetDescription()}";
        }
    }

    public readonly record struct PanelKey
    {
        public PanelColors Color { get; }
        public PanelWidths Width { get; }
        public int Length { get; }

        public const int MaxLength = 13200;

        public PanelKey(PanelColors color, PanelWidths width, int length)
        {
            if (length < 1 || MaxLength < length)
                throw new ArgumentException("Length exceeded allowed range");
            Color = color;
            Width = width;
            Length = length;
        }

        public override string ToString()
        {
            return $"{Length}mm {Color.GetDescription()} {Width.GetDescription()}";
        }
    }
}
