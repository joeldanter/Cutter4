using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Cutter4.Domain
{
    public class Configuration
    {
        public PanelKey PanelKey { get; }
        public ReadOnlyDictionary<Kapu, int> ConfigurationItems { get; }

        public int Capacity { get => PanelKey.Length; }
        public int Used { get => ConfigurationItems.Sum(x => x.Key.Length * x.Value); }
        public int Waste { get => Capacity - Used; }

        public Configuration(PanelKey panelKey, Dictionary<Kapu, int> configurationItems)
        {
            if (configurationItems.Values.Any(c => c <= 0))
                throw new ArgumentException("A configuration can not contain an item 0 or less times");
            if (panelKey.Length < configurationItems.Sum(x => x.Key.Length * x.Value))
                throw new ArgumentException("Configuration items' length exceeded its capacity");
            if (configurationItems.Any(e => e.Key.Color != panelKey.Color))
                throw new ArgumentException("Kapu color has to match configuration color");

            PanelKey = panelKey;
            ConfigurationItems = configurationItems.AsReadOnly();
        }

        public override string ToString()
        {
            StringBuilder builder = new();
            foreach (KeyValuePair<Kapu, int> entry in ConfigurationItems)
            {
                int length = entry.Key.Length;
                string serialNumber = entry.Key.SerialNumber;
                int count = entry.Value;

                for (int _ = 0; _ < count; _++)
                {
                    builder.Append($"{length}-{serialNumber} ");
                }
            }
            builder.Remove(builder.Length - 1, 1);
            return builder.ToString();
        }
    }
}
