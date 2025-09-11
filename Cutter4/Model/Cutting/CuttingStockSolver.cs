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
        PanelCollection<Configuration> Solve(PanelCollection<KapuPanel> panelCollectionToCut, Dictionary<Panel, int> storagePanelCounts);
    }

    public class GreedySolver : ICuttingStockSolver
    {
        public GreedySolver()
        {

        }

        public PanelCollection<Configuration> Solve(PanelCollection<KapuPanel> panelCollectionToCut, Dictionary<Panel, int> storagePanelCounts)
        {
            PanelCollection<Configuration> resultConfigurations = new PanelCollection<Configuration>();

            foreach (KeyValuePair<PanelType, List<KapuPanel>> entry in panelCollectionToCut)
            {
                PanelType type = entry.Key;
                List<KapuPanel> panelsToCut = new List<KapuPanel>(entry.Value);
                Dictionary<int, int> storagePanelCountsOfType = storagePanelCounts
                    .Where(x => x.Key.Type == type)
                    .Select(x => new KeyValuePair<int, int>(x.Key.Length, x.Value))
                    .ToDictionary();

                while (0 < panelsToCut.Count)
                {
                    // greedy ahhh
                    Configuration newConfiguration = ConfigurationWithLeastWasteUsingStoragePanels(panelsToCut, storagePanelCountsOfType, type);

                    // save the new configuration
                    resultConfigurations[type].Add(newConfiguration);

                    // remove cut panels from panelsToCut
                    foreach (KapuPanel toRemove in newConfiguration.Panels)
                    {
                        panelsToCut.Remove(toRemove);
                    }

                    // remove the panel from storagePanels which was used to cut the new configuration from
                    storagePanelCountsOfType[newConfiguration.Capacity] -= 1;                    
                }
            }

            return resultConfigurations;
        }
        
        Configuration ConfigurationWithLeastWasteUsingStoragePanels(IList<KapuPanel> panelsToCut, Dictionary<int, int> storagePanelCounts, PanelType type)
        {
            Configuration bestConfiguration = null;
            int leastWaste = int.MaxValue;

            foreach (KeyValuePair<int, int> storagePanel in storagePanelCounts)
            {
                int length = storagePanel.Key;
                int count = storagePanel.Value;
                if (count > 0)
                {
                    Configuration newConfiguration = Configuration.BestConfiguration(panelsToCut, length);

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
