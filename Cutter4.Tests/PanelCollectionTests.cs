using Cutter4.Model.Cutting;
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
            PanelCollection<KapuPanel> collection = new PanelCollection<KapuPanel>();

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
            PanelCollection<KapuPanel> collection = new PanelCollection<KapuPanel>();
            PanelType type = new PanelType("500mm", PanelColors.F201);
            KapuPanel panel = new KapuPanel(1000, new KapuSerialNumber("-"));
            List<KapuPanel> expected = new List<KapuPanel>() { panel };

            collection[type].Add(panel);

            Assert.True(collection[type].SequenceEqual(expected));
        }
    }
}
