using Cutter4.Model.Cutting;
using Cutter4.Model.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cutter4.Tests
{
    public class StorageTests
    {
        public static IEnumerable<object[]> KapukData => [[ KapuTestData.BatchedKapukData() ]];

        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(KapukData))]
        public void Storage_Reverts_RevertSaves(List<List<Kapu>> kapuBatches)
        {
            ICuttingStockSolver solver = new GreedySolver();
            Storage storage = new();
            storage.Server = "127.0.0.1";
            storage.Port = 3307;
            storage.DatabaseName = "cutter_testing";
            storage.UserName = "tester";
            storage.Password = "hoifajhfskahfkh";
            Dictionary<Panel, int> originalStorage = storage.StoragePanels();

            storage.EditStoragePanels("adding", PanelType.GetAllTypes()
                .Select(type => new KeyValuePair<Panel, int>(
                    new Panel(type, Kapu.MaxLength),
                    1000))
                .ToDictionary());
            foreach (List<Kapu> kapuBatch in kapuBatches)
            {
                PanelCollection<KapuPanel> panels = Kapu.ProcessPanels(kapuBatch);
                PanelCollection<Configuration> confs = solver.Solve(panels, storage.StoragePanels());
                storage.SaveNewCut(kapuBatch, confs);
            }
            for (int i = 0; i < kapuBatches.Count; i++)
            {
                storage.UndoLastSavedCut();
            }
            storage.EditStoragePanels("adding", PanelType.GetAllTypes()
                .Select(type => new KeyValuePair<Panel, int>(
                    new Panel(type, Kapu.MaxLength),
                    -1000))
                .ToDictionary());
            Dictionary<Panel, int> newStorage = storage.StoragePanels();

            Assert.True(newStorage.ToHashSet().SetEquals(originalStorage));
        }
    }
}
