using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cutter4.Domain.Cutting
{
    public class CutConfiguration
    {
        public int Capacity { get; }
        public List<int> Items { get; }

        public CutConfiguration(int capacity, List<int> items)
        {
            if (capacity < 1)
                throw new ArgumentException("The capacity must be at least 1");
            if (items.Any(x => x < 1))
                throw new ArgumentException("All items need to have a length of at least 1");
            if (capacity < items.Sum())
                throw new ArgumentException("The sum of the items can not exceed the capacity");

            Capacity = capacity;
            Items = items;
        }

        public int Used
        {
            get => Items.Sum();
        }
        public int Waste
        {
            get => Capacity - Used;
        }

        public CutConfiguration Copy()
        {
            return new CutConfiguration(Capacity, [.. Items]);
        }

        public static CutConfiguration BestConfiguration(IList<int> values, int capacity)
        {
            if (values.All(x => x > capacity))
                return new CutConfiguration(capacity, []);

            int N = values.Count;

            // Initialize a table where individual rows represent items
            // and columns represent the weight of the knapsack
            int[,] DP = new int[N + 1, capacity + 1];
            int sz;

            for (int i = 1; i <= N; i++)
            {
                // Get the value of the item
                int v = values[i - 1];

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
            List<int> itemsSelected = [];

            // Using the information inside the table we can backtrack and determine
            // which items were selected during the dynamic programming phase. The idea
            // is that if DP[i][sz] != DP[i-1][sz] then the item was selected
            for (int i = N; i > 0; i--)
            {
                if (DP[i, sz] != DP[i - 1, sz])
                {
                    int itemIndex = i - 1;
                    itemsSelected.Add(values[itemIndex]);
                    sz -= values[itemIndex];
                }
            }

            return new CutConfiguration(capacity, itemsSelected);
        }
    }
}
