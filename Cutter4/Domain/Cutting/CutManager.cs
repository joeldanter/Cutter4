using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cutter4.Domain.Cutting
{
    public class CutManager
    {
        readonly ICuttingStockSolver solver;

        public CutManager(ICuttingStockSolver solver)
        {
            this.solver = solver;
        }

        public Dictionary<PanelType, List<Configuration>> Solve(IDictionary<PanelType, List<Kapu>> toCut, IDictionary<PanelKey, int> storage)
        {
            Dictionary<PanelType, List<Configuration>> result = [];

            foreach (KeyValuePair<PanelType, List<Kapu>> entry in toCut)
            {
                PanelType type = entry.Key;

                List<int> toCutOfType = [.. entry.Value.Select(x => x.Length)];
                Dictionary<int, int> storageOfType = storage
                    .Where(x => x.Key.Color == type.Color && x.Key.Width == type.Width)
                    .ToDictionary(x => x.Key.Length, x => x.Value);

                IList<CutConfiguration>? solution = solver.Solve(toCutOfType, storageOfType) ?? throw DomainErrors.CutSolve_InsufficientStock(type);

                Dictionary<int, List<Kapu>> kapuk = entry.Value
                    .GroupBy(x => x.Length)
                    .ToDictionary(g => g.Key, g => g.Reverse().ToList());
                foreach (CutConfiguration conf in solution)
                {
                    Dictionary<Kapu, int> decoratedItems = [];
                    foreach (int item in conf.Items)
                    {
                        Kapu kapu = kapuk[item].Last();
                        if (!decoratedItems.ContainsKey(kapu))
                            decoratedItems[kapu] = 0;
                        decoratedItems[kapu] += 1;

                        kapuk[item].RemoveAt(kapuk[item].Count - 1);
                    }
                    Configuration newConfiguration = new(new PanelKey(type.Color, type.Width, conf.Capacity), decoratedItems);

                    if (!result.ContainsKey(type))
                        result[type] = [];
                    result[type].Add(newConfiguration);
                }
            }

            return result;
        }
    }
}
