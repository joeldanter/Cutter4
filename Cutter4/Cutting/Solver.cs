using Cutter4.DataBase;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cutter4.Cutting
{
    class Solver
    {
        public Solver()
        {
            
        }

        public PanelCollection<Configuration> Solve(IList<Kapu> kapuk, Storage storage)
        {
            PanelCollection<Panel> panels = Kapu.ProcessPanels(kapuk);
            PanelCollection<Configuration> configurations = new PanelCollection<Configuration>();

            foreach (KeyValuePair<PanelType, List<Panel>> entry in panels)
            {
                PanelType type = entry.Key;
                List<Panel> panelsToCut = entry.Value;
                Dictionary<int, int> usedPanelCounts = new Dictionary<int, int>();

                while (0 < panelsToCut.Count)
                {
                    Configuration bestConfiguration = new Configuration(new List<Panel> { }, 99999);

                    foreach (DataRow row in storage.LeftOvers(type).Tables[0].Rows)
                    {
                        int length = Convert.ToInt32(row["length"]);
                        int count = Convert.ToInt32(row["storage_count"]) - (usedPanelCounts.ContainsKey(length) ? usedPanelCounts[length] : 0);

                        if (count > 0)
                        {
                            Configuration newConfiguration = Configuration.BestConfiguration(panelsToCut, length);
                            if (0 < newConfiguration.Panels.Count && newConfiguration.Waste <= bestConfiguration.Waste)
                                bestConfiguration = newConfiguration;
                        }
                    }

                    if (bestConfiguration.Capacity == 99999)
                    {
                        throw new InvalidOperationException("Not enough panels to cut from");
                    }

                    // mentjuk a konfiguraciot
                    configurations[type].Add(bestConfiguration);

                    // elvesszuk az elhasznalt panelt
                    foreach (Panel toRemove in bestConfiguration.Panels)
                    {
                        panelsToCut.Remove(toRemove);
                    }

                    // elmentjuk hogy felhasznaltunk egy panelt
                    if (!usedPanelCounts.ContainsKey(bestConfiguration.Capacity))
                        usedPanelCounts.Add(bestConfiguration.Capacity, 0);
                    
                    usedPanelCounts[bestConfiguration.Capacity]++;
                }
            }

            return configurations;
        }

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

                outputStr += $"{ kapu.SerialNumber} {kapu.Color} {kapu.Length}mm*{kapu.Height}mm | {mm500}x500mm, {mm610}x610mm\n";
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
