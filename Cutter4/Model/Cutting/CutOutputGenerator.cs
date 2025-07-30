using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cutter4.Model.Cutting
{
    public interface ICutOutputGenerator
    {
        string GenerateOutput(IList<Kapu> kapuk, PanelCollection<Configuration> configurations);
    }

    public class CompactOutputGenerator : ICutOutputGenerator
    {
        public string GenerateOutput(IList<Kapu> kapuk, PanelCollection<Configuration> configurations)
        {
            string outputStr = "";

            outputStr += "Dátum: " + DateTime.Now.ToString("yyyy:MM:dd HH:mm:ss") + "\n\n";

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
                    outputStr += "===== " + item.Key.Width + " " + item.Key.Color + " =====\n";

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
}
