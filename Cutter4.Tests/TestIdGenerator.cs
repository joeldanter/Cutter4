using System;
using System.Collections.Generic;
using System.Text;

namespace Cutter4.Tests
{
    internal static class TestIdGenerator
    {
        private static long _current = 0;
        public static long NextId() => Interlocked.Increment(ref _current);
    }
}
