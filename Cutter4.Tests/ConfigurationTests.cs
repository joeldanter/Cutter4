using Cutter4.Cutting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Xsl;
using Xunit;

namespace Cutter4.Tests
{
    public class ConfigurationTests
    {
        [Fact]
        public void Configuration_GivenNullPanelsList_ThrowsArgumentNullException()
        {
            List<Panel> panels = null;

            Action action = () => new Configuration(panels, 13200);

            Assert.Throws<ArgumentNullException>(action);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(0, 1)]
        public void Configuration_GivenNegativeOrZeroCapacity_ThrowsArgumentException(int capacity, params int[] lengths)
        {
            List<Panel> panels = new List<Panel>();
            foreach (int length in lengths)
            {
                Panel panel = new Panel(length, "-");
                panels.Add(panel);
            }

            Action action = () => new Configuration(panels, capacity);

            Assert.Throws<ArgumentException>(action);
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(1, 1, 1)]
        [InlineData(100, 10, 80, 10, 2)]
        public void Configuration_GivenSmallerCapacityThenUsedLength_ThrowsArgumentException(int capacity, params int[] lengths)
        {
            List<Panel> panels = new List<Panel>();
            foreach (int length in lengths)
            {
                Panel panel = new Panel(length, "-");
                panels.Add(panel);
            }

            Action action = () => new Configuration(panels, capacity);

            Assert.Throws<ArgumentException>(action);
        }

        [Theory]
        [InlineData(5380, 1000, 4000, 380)]
        [InlineData(10640, 1000, 4000, 3420, 2220)]
        [InlineData(0)]
        [InlineData(1, 1)]
        [InlineData(4, 1, 1, 1, 1)]
        public void Used_SetPanels_ReturnsUsedLength(int expectedUsedLength, params int[] lengths)
        {
            List<Panel> panels = new List<Panel>();
            foreach (int length in lengths)
            {
                Panel panel = new Panel(length, "-");
                panels.Add(panel);
            }
            Configuration configuration = new Configuration(panels, 13200);

            int usedLength = configuration.Used;

            Assert.Equal(expectedUsedLength, usedLength);
        }

        [Fact]
        public void BestConfiguration_GivenNullList_ThrowsArgumentNullException()
        {
            List<Panel> values = null;

            Action action = () => Configuration.BestConfiguration(values, 13200);

            Assert.Throws<ArgumentNullException>(action);
        }
    }
}
