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
using Xunit.Abstractions;
using FluentAssertions;

namespace Cutter4.Tests
{
    public class SolverTests
    {
        ITestOutputHelper outputHelper;

        public SolverTests(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        static IEnumerable<Kapu> ReadData()
        {
            using StreamReader sr = new(Path.Combine(GetProjectRoot(), "kaputraindata.csv"));

            for (int i = 0; !sr.EndOfStream; i++)
            {
                string line = sr.ReadLine();
                string[] splitted_line = line.Split(';');
                int length = int.Parse(splitted_line[0]);
                int height = int.Parse(splitted_line[1]);
                Enum.TryParse(splitted_line[2], out PanelColors color);

                yield return new Kapu(length, height, color, $"kapu{i}");
            }
        }

        static string GetProjectRoot([CallerFilePath] string thisFile = "")
            => Path.GetDirectoryName(thisFile)!;

        static IEnumerable<Kapu> GenerateDemand(List<Kapu> kapuk, Random r)
        {
            int batchSize = Math.Min(r.Next(5, 6), kapuk.Count);

            for (int n = 0; n < batchSize; n++)
                yield return kapuk[r.Next(kapuk.Count)];
        }

        public static IEnumerable<object[]> SolversData =>
            [
                [new GreedySolver()],
                [new LPSolver(LPSolver.FlatLoss)],
                [new LPSolver(LPSolver.BumpedLoss)],
            ];

        [Theory]
        [MemberData(nameof(SolversData))]
        public void SolverComparisons(ICuttingStockSolver solver)
        {
            Random r = new(820);
            List<Kapu> kapuData = ReadData().ToList();
            Dictionary<int, int> startStorage = new() { { 13200, 1000 } };
            double repWaste = 0;

            int reps = 5;
            int orders = 10;
            for (int rep = 0; rep < reps; rep++)
            {
                outputHelper.WriteLine("");
                outputHelper.WriteLine($"===== {rep+1}. repetition =====");
                outputHelper.WriteLine("");
                outputHelper.WriteLine($"Start storage: {string.Join(", ", startStorage.Select(x => x.Key + ": " + x.Value))}");

                Dictionary<int, int> storage = new(startStorage);
                int sumWaste = 0;

                for (int order = 0; order < orders; order++ )
                {
                    List<Kapu> demand = GenerateDemand(kapuData, r).ToList();
                    List<int> panels = demand.SelectMany(x => { x.GetPanelWidths(out int a, out int b); return Enumerable.Repeat(x.Length, a); }).ToList();
                    List<CutConfiguration>? configurations = solver.Solve(panels, storage);

                    Assert.NotNull(configurations);
                    configurations.SelectMany(c => c.Items).Should().BeEquivalentTo(panels);

                    foreach (CutConfiguration conf in configurations)
                    {
                        storage[conf.Capacity]--;

                        if (conf.Waste > 0)
                            storage[conf.Waste] = storage.GetValueOrDefault(conf.Waste, 0) + 1;
                    }

                    Assert.True(storage.All(x => x.Value >= 0));

                    int waste = configurations.Sum(c => c.Waste) - configurations.Where(c => c.Capacity != 13200).Sum(x => configurations.Capacity);
                    sumWaste += waste;
                    outputHelper.WriteLine($"Demand: {string.Join(", ", panels.OrderBy(l => -l))}");
                    outputHelper.WriteLine($"Configurations: {string.Concat(configurations.OrderBy(c => c.Waste).Select(c => "\n\t" + c.ToDebugString()))}");
                    outputHelper.WriteLine($"New storage: {string.Join(", ", storage.Where(x => x.Value > 0).OrderBy(x => -x.Key).Select(x => x.Key+": "+x.Value))}");
                    outputHelper.WriteLine($"Delta waste: {waste}");
                    outputHelper.WriteLine("");
                }
                
                repWaste += sumWaste;
                outputHelper.WriteLine($"{rep+1}. repetition summed added waste: {sumWaste}");
                outputHelper.WriteLine("");
            }

            outputHelper.WriteLine($"Average added waste: {repWaste/reps}");
        }
    }
}
