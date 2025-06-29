using Cutter4.Cutting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Cutter4.Tests
{
    public class PanelCollectionTests
    {
        [Fact]
        public void PanelCollection_Initialized_CreatesAllEntries()
        {
            PanelCollection<Panel> collection = new PanelCollection<Panel>();

            int i = 0;
            foreach (var item in collection)
            {
                i++;
            }

            Assert.Equal(Enum.GetValues(typeof(PanelColors)).Length*2, i);
        }

        [Fact]
        public void Add_Element_AddsElement()
        {
            PanelCollection<Panel> collection = new PanelCollection<Panel>();
            PanelType type = new PanelType("500mm", PanelColors.F201);
            Panel panel = new Panel(1000, "-");
            List<Panel> expected = new List<Panel>() { panel };

            collection[type].Add(panel);

            Assert.True(collection[type].SequenceEqual(expected));
        }
    }
}
