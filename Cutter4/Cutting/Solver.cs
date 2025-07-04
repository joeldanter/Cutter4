using Cutter4.DataBase;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cutter4.Cutting
{
    public class Solver
    {
        public Solver()
        {
            
        }

        public PanelCollection<Configuration> Solve(PanelCollection<KapuPanel> panelCollectionToCut, IStorage storage)
        {
            PanelCollection<Configuration> resultConfigurations = new PanelCollection<Configuration>();

            foreach (KeyValuePair<PanelType, List<KapuPanel>> entry in panelCollectionToCut)
            {
                PanelType type = entry.Key;
                List<KapuPanel> panelsToCut = new List<KapuPanel>(entry.Value);
                List<StoragePanel> storagePanels = storage.LeftOvers(type);

                while (0 < panelsToCut.Count)
                {
                    // greedy ahhh
                    Configuration newConfiguration = ConfigurationWithLeastWasteUsingStoragePanels(panelsToCut, storagePanels, type);

                    // save the new configuration
                    resultConfigurations[type].Add(newConfiguration);

                    // remove cut panels from panelsToCut
                    foreach (KapuPanel toRemove in newConfiguration.Panels)
                    {
                        panelsToCut.Remove(toRemove);
                    }

                    // remove the panel from storagePanels which was used to cut the new configuration from
                    for (int i = 0; i < storagePanels.Count; i++)
                    {
                        if (storagePanels[i].Length == newConfiguration.Capacity)
                        {
                            StoragePanel usedStoragePanel = storagePanels[i];
                            usedStoragePanel.Count -= 1;
                            storagePanels[i] = usedStoragePanel;
                            break;
                        }
                    }
                }
            }

            return resultConfigurations;
        }

        Configuration ConfigurationWithLeastWasteUsingStoragePanels(List<KapuPanel> panelsToCut, List<StoragePanel> storagePanels, PanelType type)
        {
            Configuration bestConfiguration = null;
            int leastWaste = int.MaxValue;

            foreach (StoragePanel storagePanel in storagePanels)
            {
                if (storagePanel.Count > 0)
                {
                    Configuration newConfiguration = Configuration.BestConfiguration(panelsToCut, storagePanel.Length);

                    if (0 < newConfiguration.Panels.Count && newConfiguration.Waste < leastWaste)
                        bestConfiguration = newConfiguration;
                }
            }

            if (bestConfiguration is null)
            {
                throw new InvalidOperationException($"Cannot cut {panelsToCut.Count} remaining panels of type: {type.Color}, {type.Width} — no available storage panels to cut from");
            }

            return bestConfiguration;
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
