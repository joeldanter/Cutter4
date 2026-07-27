using Cutter4.Domain.Cutting;
using Cutter4.Domain;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Cutter4.Tests
{
    public static class KapuTestData
    {
        public static List<Kapu> AllKapukData()
        {
            List<Kapu> result = [];
            StreamReader sr = new(Path.Combine(GetProjectRoot(), "KapuTestData.csv"));

            for (int i = 0; !sr.EndOfStream; i++)
            {
                string line = sr.ReadLine();
                string[] splitted_line = line.Split(';');
                int length = int.Parse(splitted_line[0]);
                int height = int.Parse(splitted_line[1]);
                Enum.TryParse(splitted_line[2], out PanelColors color);
                int count = int.Parse(splitted_line[3]);

                for (int j = 0; j < count; j++)
                {
                    Kapu kapu = new Kapu(length, height, color, $"kapu{i}{(count == 1 ? "" : "-" + j)}");
                    result.Add(kapu);
                }
            }

            return result;
        }

        public static List<List<Kapu>> BatchedKapukData()
        {
            List<List<Kapu>> result = [];
            List<Kapu> kapuk = AllKapukData();
            Random r = new(820);

            for (int batchNumber = 0; batchNumber < 10000; batchNumber++)
            {
                int batchSize = Math.Min(r.Next(1, 20), kapuk.Count);
                List<Kapu> batchItems = new(batchSize);

                for (int n = 0; n < batchSize; n++)
                {
                    int index = r.Next(kapuk.Count);
                    batchItems.Add(kapuk[index]);
                }
                result.Add(batchItems);
            }

            return result;
        }

        private static string GetProjectRoot([CallerFilePath] string thisFile = "")
            => Path.GetDirectoryName(thisFile)!;
    }

    public class SolverTests
    {
        public static IEnumerable<object[]> PanelsData => [..KapuTestData.BatchedKapukData().Select(x => new object[] { Kapu.ProcessPanels(x) })];

        Dictionary<PanelKey, int> StoragePanels()
        {
            Dictionary<PanelKey, int> result = [];

            foreach (PanelType type in PanelType.GetAllTypes())
            {
                //result[new PanelKey(type.Color, type.Width, 13200)] = 100;
                //result[new PanelKey(type.Color, type.Width, 11000)] = 30;
                result[new PanelKey(type.Color, type.Width, 8000)] = 100;
                //result[new PanelKey(type.Color, type.Width, 4000)] = 7;
                result[new PanelKey(type.Color, type.Width, 3102)] = 7;
                //result[new PanelKey(type.Color, type.Width, 2000)] = 19;
            }

            return result;
        }

        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PanelsData))]
        public void GreedySolve_GivenPanels_UsesThosePanels(Dictionary<PanelType, List<Kapu>> toCut)
        {
            CutManager cutManager = new (new GreedySolver());
            Dictionary<PanelKey, int> storage = StoragePanels();

            Dictionary<PanelType, List<Configuration>> generatedConfigurations = cutManager.Solve(toCut, storage);

            Assert.True(DoConfigurationsUsePanels(generatedConfigurations, toCut));
            Assert.True(IsStorageUsageLegal(generatedConfigurations, storage));
        }

        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PanelsData))]
        public void LPSolve_GivenPanels_UsesThosePanels(Dictionary<PanelType, List<Kapu>> toCut)
        {
            CutManager cutManager = new(new LPSolver());
            Dictionary<PanelKey, int> storage = StoragePanels();

            Dictionary<PanelType, List<Configuration>> generatedConfigurations = cutManager.Solve(toCut, storage);

            Assert.True(DoConfigurationsUsePanels(generatedConfigurations, toCut));
            Assert.True(IsStorageUsageLegal(generatedConfigurations, storage));
        }

        static bool DoConfigurationsUsePanels(Dictionary<PanelType, List<Configuration>> generatedConfs, Dictionary<PanelType, List<Kapu>> toCut)
        {
            foreach (KeyValuePair<PanelType, List<Configuration>> entry in generatedConfs)
            {
                List<Kapu> generatedPanels = [.. entry.Value.SelectMany(c => c.ConfigurationItems).SelectMany(i => Enumerable.Repeat(i.Key, i.Value))];
                List<Kapu> originalPanels = toCut[entry.Key];
                if (!AreMultisetsEqual(generatedPanels, originalPanels)) return false;
            }
            return true;
        }

        static bool AreMultisetsEqual<T>(IEnumerable<T> first, IEnumerable<T> second) where T : notnull
        {
            Dictionary<T, int> secondCounts = second.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());

            foreach (T item in first)
            {
                if (!secondCounts.TryGetValue(item, out int count) || count == 0)
                    return false;
                secondCounts[item] = count - 1;
            }

            return secondCounts.Values.All(c => c == 0);
        }

        static bool IsStorageUsageLegal(Dictionary<PanelType, List<Configuration>> generatedConfigurations, Dictionary<PanelKey, int> storage)
        {
            return generatedConfigurations.All(entry => entry.Value
                .GroupBy(x => x.PanelKey)
                .ToDictionary(g => g.Key, g => g.Count())
                .All(g => storage.GetValueOrDefault(g.Key, 0) - g.Value >= 0));
        }
    }
}
