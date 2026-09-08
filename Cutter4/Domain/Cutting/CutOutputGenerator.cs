using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cutter4.Domain.Cutting
{
    public interface ICutOutputGenerator
    {
        string GenerateOutput(IEnumerable<Kapu> kapuk, IDictionary<PanelType, List<Configuration>> configurations, DateTime dateOfCut);
    }

    public class CompactOutputGenerator : ICutOutputGenerator
    {
        public string GenerateOutput(IEnumerable<Kapu> kapuk, IDictionary<PanelType, List<Configuration>> configurations, DateTime dateOfCut)
        {
            string outputStr = "";

            outputStr += "Dátum: " + dateOfCut.ToString() + "\n\n";

            // Kapuk
            outputStr += "Kapuk:\n";
            outputStr += "Szám Szín hossz*magasság | panelok\n";
            foreach (Kapu kapu in kapuk)
            {
                kapu.GetPanelWidths(out int mm500, out int mm610);

                outputStr += $"{kapu.SerialNumber} {kapu.Color} {kapu.Length}mm*{kapu.Height}mm | {mm500}x500mm, {mm610}x610mm\n";
            }
            outputStr += "\n";

            // Panelok
            outputStr += "Vágások:\n";
            outputStr += "hossz -> maradék | konfigurációk\n";
            foreach (KeyValuePair<PanelType, List<Configuration>> item in configurations)
            {
                if (0 < item.Value.Count)
                {
                    outputStr += "===== " + item.Key.Width.GetDescription() + " " + item.Key.Color + " =====\n";

                    foreach (Configuration conf in item.Value)
                    {
                        outputStr += conf.Capacity + "mm -> " + conf.Waste + "mm | " + conf.ToString() + "\n";
                    }
                    outputStr += "\n";
                }
            }

            return outputStr;
        }
    }

    class LongOutputGenerator : ICutOutputGenerator
    {
        public string GenerateOutput(IEnumerable<Kapu> kapuk, IDictionary<PanelType, List<Configuration>> configurations, DateTime dateOfCut)
        {
            StringBuilder output = new();
            
            output.AppendLine("Dátum: " + dateOfCut.ToString() + "\n");

            // Kapuk
            output.AppendLine("# Kapuk");
            foreach (Kapu kapu in kapuk)
            {
                kapu.GetPanelWidths(out int mm500, out int mm610);

                output.AppendLine($"{kapu.SerialNumber} {kapu.Length}*{kapu.Height} {kapu.Color} | {mm500}x500mm, {mm610}x610mm");
            }
            output.AppendLine();

            // Panels
            output.AppendLine("# Vágások");
            foreach (KeyValuePair<PanelType, List<Configuration>> item in configurations)
            {
                foreach (Configuration conf in item.Value)
                {
                    output.AppendLine($"Szín: {item.Key.Color}");
                    output.AppendLine($"Szélesség: {item.Key.Width.GetDescription()}");
                    output.AppendLine($"Hossz: {conf.Capacity}mm");

                    List<string> cutLengths = [];
                    List<string> cutSerialNumbers = [];
                    foreach (KeyValuePair<Kapu, int> entry in conf.ConfigurationItems.OrderByDescending(x => x.Key.Length))
                    {
                        Kapu kapu = entry.Key;
                        int count = entry.Value;

                        for (int _ = 0; _ < count; _++)
                        {
                            string panelLength = kapu.Length.ToString();
                            string serialNumber = kapu.SerialNumber;
                            int padSize = Math.Max(panelLength.Length, serialNumber.Length);

                            cutLengths.Add(panelLength.PadRight(padSize));
                            cutSerialNumbers.Add(serialNumber.PadRight(padSize));
                        }
                    }
                    output.AppendLine("Kivágott hosszak: " + string.Join(" ", cutLengths));
                    output.AppendLine("Gyártási számaik: " + string.Join(" ", cutSerialNumbers));
                    
                    output.AppendLine($"Maradék: {conf.Waste}mm");
                    output.AppendLine();
                }
            }

            // Other data
            IEnumerable<Configuration> allConfs = configurations.SelectMany(x => x.Value);
            int AddedWaste = allConfs.Where(x => x.Capacity == 13200).Sum(x => x.Capacity);
            int removedWaste = allConfs.Sum(x => x.Used);
            output.AppendLine($"Összes kivágott panel hossza: {removedWaste}mm");
            output.AppendLine($"Raktáron keletkezett maradék: {AddedWaste - removedWaste}mm");

            return output.ToString();
        }
    }
}
