using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cutter4.Cutting
{
    public class Configuration
    {
        public ReadOnlyCollection<KapuPanel> Panels { get; }
        public int Capacity { get; }
        public int Used
        {
            get
            {
                int used = 0;
                foreach (KapuPanel panel in Panels)
                {
                    used += panel.Length;
                }
                return used;
            }
        }
        public int Waste
        {
            get
            {
                return Capacity - Used;
            }
        }

        public Configuration(IList<KapuPanel> panels, int capacity)
        {
            if (panels is null)
                throw new ArgumentNullException(nameof(panels) + " can not be null");
            if (capacity < 1)
                throw new ArgumentException("The capacity of a configuration can not be smaller than 1");
            if (capacity < panels.Sum(p => p.Length))
                throw new ArgumentException("The sum of a configuration's elements' lengths can not be greater than its capacity");
            
            Panels = new ReadOnlyCollection<KapuPanel>(panels);
            Capacity = capacity;
        }

        // igazabol csak lemasoltam egy csavo programjat es atirtam: https://www.youtube.com/watch?v=cJ21moQpofY
        // kiadja a legjobb konfiguraciot a megadott panelokkal
        public static Configuration BestConfiguration(IList<KapuPanel> values, int capacity)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values) + " can not be null");

            if (!values.Any(x => x.Length <= capacity))
                return new Configuration(new List<KapuPanel>(), capacity);

            int N = values.Count;

            // Initialize a table where individual rows represent items
            // and columns represent the weight of the knapsack
            int[,] DP = new int[N + 1, capacity + 1];
            int sz;

            for (int i = 1; i <= N; i++)
            {
                // Get the value of the item
                int v = values[i - 1].Length;

                for (sz = 1; sz <= capacity; sz++)
                {
                    // Consider not picking this element
                    DP[i, sz] = DP[i - 1, sz];

                    // Consider including the current element and
                    // see if this would be more profitable
                    if (sz >= v && DP[i - 1, sz - v] + v > DP[i, sz])
                        DP[i, sz] = DP[i - 1, sz - v] + v;
                }
            }

            sz = capacity;
            List<KapuPanel> itemsSelected = new List<KapuPanel>();

            // Using the information inside the table we can backtrack and determine
            // which items were selected during the dynamic programming phase. The idea
            // is that if DP[i][sz] != DP[i-1][sz] then the item was selected
            for (int i = N; i > 0; i--)
            {
                if (DP[i, sz] != DP[i - 1, sz])
                {
                    int itemIndex = i - 1;
                    itemsSelected.Add(values[itemIndex]);
                    sz -= values[itemIndex].Length;
                }
            }

            // Return the items that were selected
            //itemsSelected.Reverse();
            return new Configuration(itemsSelected, capacity);

            // Return the maximum profit
            //return DP[N, capacity];
        }

        public override string ToString()
        {
            string result = "";
            for (int i = 0; i < Panels.Count; i++)
            {
                result += Panels[i].Length + "-" + Panels[i].SerialNumber;

                if (i != Panels.Count - 1)
                {
                    result += ", ";
                }
            }

            return result;
        }
    }
}
