using Cutter4.Model.Cutting;
using Cutter4.Model.DataBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Cutter4.Tests
{
    public class GreedySolverTests
    {
        public static IEnumerable<object[]> PanelsData()
        {
            List<object[]> result = new List<object[]>();
            StreamReader sr = new StreamReader("C:\\Users\\pixxe\\source\\repos\\Cutter4\\Cutter4.Tests/KapuTestData.csv");
            Random r = new Random(820);

            List<Kapu> batchKapuk = new List<Kapu>();
            int batchCountLeft = r.Next(1, 13);
            for (int i = 1; !sr.EndOfStream; i++)
            {
                string line = sr.ReadLine();
                string[] splitted_line = line.Split(';');
                int length =  int.Parse(splitted_line[0]);
                int height = int.Parse(splitted_line[1]);
                Enum.TryParse(splitted_line[2], out PanelColors color);
                int count = int.Parse(splitted_line[3]);

                for (int j = 0; j < count; j++)
                {
                    Kapu kapu = new Kapu(length, height, color, new KapuSerialNumber($"kapu{i}"));
                    batchKapuk.Add(kapu);
                }

                batchCountLeft--;
                if (batchCountLeft == 0 || sr.EndOfStream)
                {
                    result.Add(new object[] { Kapu.ProcessPanels(batchKapuk) });
                    batchKapuk.Clear();
                    batchCountLeft = r.Next(1, 13);
                }
            }

            return result;
        }

        Dictionary<Panel, int> storagePanels()
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
        public void Solve_GivenPanels_UsesThosePanels(PanelCollection<KapuPanel> panelCollectionToCut)
        {
            GreedySolver solver = new GreedySolver();
            
            PanelCollection<Configuration> resultConfigurationsCollection = solver.Solve(panelCollectionToCut, storagePanels());

            foreach (KeyValuePair<PanelType, List<Configuration>> entry in resultConfigurationsCollection)
            {
                PanelType type = entry.Key;
                List<Configuration> resultConfigurations = entry.Value;

                List<KapuPanel> resultPanels = resultConfigurations.SelectMany(c => c.Panels).ToList();

                HashSet<KapuPanel> set = new HashSet<KapuPanel>(panelCollectionToCut[type]);
                Assert.True(set.SetEquals(resultPanels));
            }
        }
    }
}
