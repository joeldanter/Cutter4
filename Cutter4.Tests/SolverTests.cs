using Cutter4.Model.Cutting;
using Cutter4.Model.DataBase;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using Org.BouncyCastle.Asn1.BC;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            StreamReader sr = new(".\\KapuTestData.csv");

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
                    Kapu kapu = new Kapu(length, height, color, new KapuSerialNumber($"kapu{i}{(count == 1 ? "" : "-" + j)}"));
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

            while (kapuk.Count > 0)
            {
                int batchLength = Math.Min(r.Next(1, 13), kapuk.Count);
                List<Kapu> batchKapuk = kapuk.GetRange(kapuk.Count - batchLength, batchLength);
                kapuk.RemoveRange(kapuk.Count - batchLength, batchLength);
                result.Add(batchKapuk);
            }

            return result;
        }
    }

    public class SolverTests
    {
        public static IEnumerable<object[]> PanelsData => [..KapuTestData.BatchedKapukData().Select(x => new object[] { Kapu.ProcessPanels(x) })];

        Dictionary<Panel, int> StoragePanels()
        {
            Dictionary<Panel, int> result = [];

            foreach (PanelType type in PanelType.GetAllTypes())
            {
                result.Add(new Panel(type, 13200), 20);
                result.Add(new Panel(type, 11000), 8);
                result.Add(new Panel(type, 8000), 5);
                result.Add(new Panel(type, 4000), 3);
            }

            return result;
        }

        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PanelsData))]
        public void GreedySolve_GivenPanels_UsesThosePanels(PanelCollection<KapuPanel> panelCollectionToCut)
        {
            GreedySolver solver = new GreedySolver();
            
            PanelCollection<Configuration> generatedConfigurations = solver.Solve(panelCollectionToCut, StoragePanels());

            Assert.True(DoConfigurationsUsePanels(generatedConfigurations, panelCollectionToCut));
        }

        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PanelsData))]
        public void LPSolve_GivenPanels_UsesThosePanels(PanelCollection<KapuPanel> panelCollectionToCut)
        {
            LPSolver solver = new();
            
            PanelCollection<Configuration> generatedConfigurations = solver.Solve(panelCollectionToCut, StoragePanels());

            Assert.True(DoConfigurationsUsePanels(generatedConfigurations, panelCollectionToCut));
        }

        static bool DoConfigurationsUsePanels(PanelCollection<Configuration> generatedConfigurations, PanelCollection<KapuPanel> panelCollectionToCut)
        {
            foreach (KeyValuePair<PanelType, List<Configuration>> entry in generatedConfigurations)
            {
                List<KapuPanel> generatedPanels = [.. entry.Value.SelectMany(c => c.Panels)];
                List<KapuPanel> originalPanels = panelCollectionToCut[entry.Key];
                if (!DoListsHaveSameElements(generatedPanels, originalPanels)) return false;
            }
            return true;
        }

        static bool DoListsHaveSameElements<T>(IList<T> first, IList<T> second)
        {
            List<T> secondCopy = [.. second];
            foreach (T item in first)
            {
                int secondIndex = secondCopy.IndexOf(item);
                if (secondIndex == -1) return false;
                secondCopy.RemoveAt(secondIndex);
            }
            return secondCopy.Count == 0;
        }
    }
}
