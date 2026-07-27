using Cutter4.Domain.Cutting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Xsl;
using Xunit;

namespace Cutter4.Tests
{
    public class CutConfigurationTests
    {
        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(0, 1)]
        public void Configuration_GivenNegativeOrZeroCapacity_ThrowsArgumentException(int capacity, params int[] items)
        {
            Action action = () => new CutConfiguration(capacity, items.ToList());

            Assert.Throws<ArgumentException>(action);
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(1, 1, 1)]
        [InlineData(100, 10, 80, 10, 2)]
        public void Configuration_GivenSmallerCapacityThanUsedLength_ThrowsArgumentException(int capacity, params int[] items)
        {
            Action action = () => new CutConfiguration(capacity, items.ToList());

            Assert.Throws<ArgumentException>(action);
        }

        [Theory]
        [InlineData(5380, 1000, 4000, 380)]
        [InlineData(10640, 1000, 4000, 3420, 2220)]
        [InlineData(0)]
        [InlineData(1, 1)]
        [InlineData(4, 1, 1, 1, 1)]
        public void Used_SetPanels_ReturnsUsedLength(int expectedUsedLength, params int[] items)
        {
            CutConfiguration configuration = new(13200, items.ToList());

            int usedLength = configuration.Used;

            Assert.Equal(expectedUsedLength, usedLength);
        }
    }
}
