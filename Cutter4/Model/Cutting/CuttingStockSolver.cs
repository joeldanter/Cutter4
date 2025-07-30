using Cutter4.Model.DataBase;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cutter4.Model.Cutting
{
    public interface ICuttingStockSolver
    {
        PanelCollection<Configuration> Solve(PanelCollection<KapuPanel> panelCollectionToCut, PanelCollection<StoragePanel> storagePanelsCollection);
    }

    public class GreedySolver : ICuttingStockSolver
    {
        public GreedySolver()
        {

        }

        public PanelCollection<Configuration> Solve(PanelCollection<KapuPanel> panelCollectionToCut, PanelCollection<StoragePanel> storagePanelsCollection)
        {
            PanelCollection<Configuration> resultConfigurations = new PanelCollection<Configuration>();

            foreach (KeyValuePair<PanelType, List<KapuPanel>> entry in panelCollectionToCut)
            {
                PanelType type = entry.Key;
                List<KapuPanel> panelsToCut = new List<KapuPanel>(entry.Value);
                List<StoragePanel> storagePanelsOfType = storagePanelsCollection[type];

                while (0 < panelsToCut.Count)
                {
                    // greedy ahhh
                    Configuration newConfiguration = ConfigurationWithLeastWasteUsingStoragePanels(panelsToCut, storagePanelsOfType, type);

                    // save the new configuration
                    resultConfigurations[type].Add(newConfiguration);

                    // remove cut panels from panelsToCut
                    foreach (KapuPanel toRemove in newConfiguration.Panels)
                    {
                        panelsToCut.Remove(toRemove);
                    }

                    // remove the panel from storagePanels which was used to cut the new configuration from
                    for (int i = 0; i < storagePanelsOfType.Count; i++)
                    {
                        if (storagePanelsOfType[i].Length == newConfiguration.Capacity)
                        {
                            StoragePanel usedStoragePanel = storagePanelsOfType[i];
                            usedStoragePanel.Count -= 1;
                            storagePanelsOfType[i] = usedStoragePanel;
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
                    {
                        bestConfiguration = newConfiguration;
                        leastWaste = bestConfiguration.Waste;
                    }
                }
            }

            if (bestConfiguration is null)
            {
                throw new InvalidOperationException($"Cannot cut {panelsToCut.Count} remaining panels of type: {type.Color}, {type.Width} — no available storage panels to cut from");
            }

            return bestConfiguration;
        }
    }

}
