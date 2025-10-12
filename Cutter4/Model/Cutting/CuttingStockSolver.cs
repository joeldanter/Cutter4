using Cutter4.Model.DataBase;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Highs;

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

    public class LPSolver : ICuttingStockSolver
    {
        List<Configuration> PossibleConfigurations(Dictionary<int, int> demand, int maxLength)
        {
            List<List<int>> configurations = [[]];
            foreach (KeyValuePair<int, int> item in demand)
            {
                int itemLength = item.Key;
                int itemCount = item.Value;
                List<List<int>> subConfigurations = [.. configurations];
                configurations = [];
                foreach(List<int> subConfiguration in subConfigurations)
                {
                    int remainingLength = maxLength - subConfiguration.Sum();
                    int maxAddableCount = Math.Min(remainingLength / itemLength, itemCount);
                    for (int countToAdd = 0; countToAdd <= maxAddableCount; countToAdd++)
                    {
                        List<int> newItems = [.. subConfiguration];
                        newItems.AddRange(Enumerable.Repeat(itemLength, countToAdd));
                        configurations.Add(newItems);
                    }
                }
            }

            configurations = [.. configurations.Where(x => x.Count > 0)];
            return [.. configurations
                .Select(numbers =>
                    new Configuration(
                        [.. numbers.Select(number =>
                            new KapuPanel(number,
                            new KapuSerialNumber("-")))],
                        maxLength))];
        }

        public PanelCollection<Configuration> Solve(PanelCollection<KapuPanel> panelCollectionToCut, Dictionary<Panel, int> storagePanelCounts)
        {
            PanelCollection<Configuration> resultConfigurations = [];

            foreach (KeyValuePair<PanelType, List<KapuPanel>> entry in panelCollectionToCut)
            {
                PanelType type = entry.Key;
                Dictionary<int, int> demand = [];
                foreach (KapuPanel panel in entry.Value)
                {
                    int length = panel.Length;
                    if (!demand.ContainsKey(length))
                    {
                        demand[length] = 0;
                    }
                    demand[length]++;
                }
                Dictionary<int, int> storageCountsOfType = storagePanelCounts
                    .Where(x => x.Key.Type == type)
                    .Select(x => new KeyValuePair<int, int>(x.Key.Length, x.Value))
                    .ToDictionary();

                List<Configuration> possibleConfs = [.. storageCountsOfType.Keys.SelectMany(panelLength => PossibleConfigurations(demand, panelLength))];

                List<int> storageLengths = [..storageCountsOfType.Keys];
                List<int> storageCounts = [.. storageLengths.Select(length => storageCountsOfType[length])];
                List<int> demandLengths = [.. demand.Keys];
                List<int> demandCounts = [.. demandLengths.Select(length => demand[length])];

                int NumColums = possibleConfs.Count;
                int numRows = demand.Count + storageCountsOfType.Count;
                double[] costs = [.. possibleConfs.Select(DeltaLoss)];
                double[] variableLowerBounds = [.. Enumerable.Repeat(0, NumColums)];
                double[] variableUpperBounds = [.. Enumerable.Repeat(1e30, NumColums)];
                double[] constraintLowerBounds = [.. demandCounts.Concat(Enumerable.Repeat(0, storageCountsOfType.Count))];
                double[] constraintUpperBounds = [.. demandCounts.Concat(storageCounts)];
                List<int> astart = [];
                List<int> aindex = [];
                List<double> avalue = [];
                for (int i = 0; i < NumColums; i++)
                {
                    astart.Add(avalue.Count);
                    for (int j = 0; j < demand.Count; j++)
                    {
                        int count = possibleConfs[i].Panels.Count(item => item.Length == demandLengths[j]);
                        if (count > 0)
                        {
                            aindex.Add(j);
                            avalue.Add(count);
                        }
                    }
                    aindex.Add(demand.Count + storageLengths.IndexOf(possibleConfs[i].Capacity));
                    avalue.Add(1);
                }
                astart.Add(numRows * NumColums - avalue.Count);
                int[] integrality = [.. Enumerable.Repeat((int)HighsIntegrality.kInteger, NumColums)];

                HighsModel model = new(costs, variableLowerBounds, variableUpperBounds, constraintLowerBounds, constraintUpperBounds, [.. astart], [.. aindex], [.. avalue], integrality);
                HighsLpSolver solver = new();
                HighsStatus status1 = solver.passMip(model);
                HighsStatus status2 = solver.run();
                HighsSolution solution = solver.getSolution();

                List<KapuPanel> panelsToCut = [.. entry.Value];
                for (int i = 0; i < solution.colvalue.Length; i++)
                {
                    List<KapuPanel> newPanels = possibleConfs[i].Panels;
                    int count = Convert.ToInt32(solution.colvalue[i]);
                    for (int _ = 0; _ < count; _++)
                    {
                        for (int panel_i = 0; panel_i < newPanels.Count; panel_i++)
                        {
                            int j = panelsToCut.FindIndex(x => x.Length == newPanels[panel_i].Length);
                            newPanels[panel_i] = new KapuPanel(newPanels[panel_i].Length, panelsToCut[j].SerialNumber);
                            panelsToCut.RemoveAt(j);
                        }
                        Configuration newConfiguration = new Configuration(possibleConfs[i].Panels, possibleConfs[i].Capacity);
                        resultConfigurations[type].Add(newConfiguration);
                    }
                }
            }

            return resultConfigurations;
        }

        double DeltaLoss(Configuration conf)
        {
            if (conf.Waste < 1600)
                return 2.5 * conf.Waste;
            if (conf.Waste < 3200)
                return 4960 - (0.6 * conf.Waste);
            if (conf.Waste < 13200)
                return 2081 + 0.3 * conf.Waste;
            return 0;
        }
    }
}
