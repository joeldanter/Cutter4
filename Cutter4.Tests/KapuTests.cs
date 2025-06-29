using Cutter4.Cutting;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Cutter4.Tests
{
    public class KapuTests
    {
        [Theory]
        [InlineData(550, 1, 0)]
        [InlineData(551, 0, 1)]
        [InlineData(660, 0, 1)]
        [InlineData(1600, 0, 3)]
        [InlineData(2000, 4, 0)]
        [InlineData(3000, 0, 5)]
        [InlineData(4000, 2, 5)]
        [InlineData(5000, 4, 5)]
        [InlineData(5210, 3, 6)]
        public void GetPanelWidths_SetHeight_ReturnsCorrectWidths(int height, int expected_mm500, int expected_mm610)
        {
            Kapu kapu = new Kapu() { Height = height };

            kapu.GetPanelWidths(out int mm500, out int mm610);

            Assert.Equal(mm500, expected_mm500);
            Assert.Equal(mm610, expected_mm610);
        }

        [Fact]
        public void ProcessPanels_GivenEmtpyList_ReturnsEmptyCollection()
        {
            List<Kapu> kapuk = new List<Kapu>();

            PanelCollection<Panel> panels = Kapu.ProcessPanels(kapuk);

            foreach (KeyValuePair<PanelType, List<Panel>> entry in panels)
            {
                Assert.Empty(panels[entry.Key]);
            }
        }

        [Fact]
        public void ProcessPanels_GivenNullList_ThrowsException()
        {
            List<Kapu> kapuk = null;

            Action action = () => Kapu.ProcessPanels(kapuk);

            Assert.Throws<ArgumentNullException>(action);
        }
    }
}