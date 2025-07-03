using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cutter4.DataBase
{
    public struct StoragePanel
    {
        public int Length { get; set; }
        public int Count { get; set; }
        
        public StoragePanel(int length, int count)
        {
            Length = length;
            Count = count;
        }
    }
}
