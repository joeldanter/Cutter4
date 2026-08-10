using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using Highs;

namespace Cutter4.Domain.Cutting
{
    public interface ICuttingStockSolver
    {
        List<CutConfiguration>? Solve(IList<int> toCut, IDictionary<int, int> storage);
    }

    public class GreedySolver : ICuttingStockSolver
    {
        public List<CutConfiguration>? Solve(IList<int> toCut, IDictionary<int, int> storage)
        {
            toCut = [.. toCut];
            storage = new Dictionary<int, int>(storage);
            List<CutConfiguration> result = [];

            while (0 < toCut.Count)
            {
                // greedy ahhh
                CutConfiguration? newConfiguration = ConfigurationWithLeastWaste(toCut, storage);
                
                if (newConfiguration == null)
                    return null;

                result.Add(newConfiguration);

                foreach (int toRemove in newConfiguration.Items)
                {
                    toCut.Remove(toRemove);
                }

                storage[newConfiguration.Capacity] -= 1;
            }

            return result;
        }

        CutConfiguration? ConfigurationWithLeastWaste(IList<int> toCut, IDictionary<int, int> storage)
        {
            CutConfiguration? bestConfiguration = null;

            foreach (KeyValuePair<int, int> storageItem in storage)
            {
                int length = storageItem.Key;
                int count = storageItem.Value;
                if (count > 0)
                {
                    CutConfiguration newConfiguration = CutConfiguration.BestConfiguration(toCut, length);

                    if (0 < newConfiguration.Items.Count && (bestConfiguration is null || newConfiguration.Waste < bestConfiguration.Waste))
                    {
                        bestConfiguration = newConfiguration;
                    }
                }
            }

            return bestConfiguration;
        }
    }

    public class LPSolver : ICuttingStockSolver
    {
        public List<CutConfiguration>? Solve(IList<int> toCut, IDictionary<int, int> storage)
        {
            Dictionary<int, int> demand = toCut
                .GroupBy(x => x)
                .ToDictionary(g => g.Key, g => g.Count());

            List<CutConfiguration> possibleConfs = [.. storage.Keys.SelectMany(length => PossibleConfigurations(demand, length))];

            List<int> storageLengths = [.. storage.Keys];
            List<int> storageCounts = [.. storageLengths.Select(length => storage[length])];
            List<int> demandLengths = [.. demand.Keys];
            List<int> demandCounts = [.. demandLengths.Select(length => demand[length])];

            int numColums = possibleConfs.Count;
            int numRows = demand.Count + storage.Count;
            double[] costs = [.. possibleConfs.Select(DeltaLoss)];
            double[] variableLowerBounds = [.. Enumerable.Repeat(0, numColums)];
            double[] variableUpperBounds = [.. Enumerable.Repeat(double.PositiveInfinity, numColums)];
            double[] constraintLowerBounds = [.. demandCounts.Concat(Enumerable.Repeat(0, storage.Count))];
            double[] constraintUpperBounds = [.. demandCounts.Concat(storageCounts)];
            List<int> astart = [];
            List<int> aindex = [];
            List<double> avalue = [];
            for (int i = 0; i < numColums; i++)
            {
                astart.Add(avalue.Count);
                for (int j = 0; j < demand.Count; j++)
                {
                    int count = possibleConfs[i].Items.Count(item => item == demandLengths[j]);
                    if (count > 0)
                    {
                        aindex.Add(j);
                        avalue.Add(count);
                    }
                }
                aindex.Add(demand.Count + storageLengths.IndexOf(possibleConfs[i].Capacity));
                avalue.Add(1);
            }
            //astart.Add(avalue.Count);
            astart.Add(numRows * numColums - avalue.Count);
            int[] integrality = [.. Enumerable.Repeat((int)HighsIntegrality.kInteger, numColums)];

            HighsLpSolver solver = new();
            HighsModel model = new(costs, variableLowerBounds, variableUpperBounds, constraintLowerBounds, constraintUpperBounds, [.. astart], [.. aindex], [.. avalue], integrality);
            solver.passMip(model);
            solver.run();
            HighsModelStatus status = solver.GetModelStatus();
            if (status != HighsModelStatus.kOptimal)
                return null;

            HighsSolution solution = solver.getSolution();
            List<CutConfiguration> result = [];
            for (int i = 0; i < solution.colvalue.Length; i++)
            {
                int count = Convert.ToInt32(solution.colvalue[i]);

                for (int _ = 0; _ < count; _++)
                {
                    result.Add(possibleConfs[i].Copy());
                }
            }
            return result;
        }

        List<CutConfiguration> PossibleConfigurations(Dictionary<int, int> demand, int maxLength)
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

            return [.. configurations
                .Where(x => x.Count > 0)
                .Select(numbers => new CutConfiguration(maxLength, [.. numbers]))];
        }

        double DeltaLoss(CutConfiguration conf)
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
