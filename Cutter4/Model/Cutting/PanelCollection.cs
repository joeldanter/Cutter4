using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cutter4.Model.Cutting
{
    public class PanelCollection<T> : IEnumerable<KeyValuePair<PanelType, List<T>>>
    {
        Dictionary<PanelType, List<T>> panels;

        public PanelCollection()
        {
            panels = [];

            foreach (PanelType type in PanelType.GetAllTypes())
            {
                panels.Add(type, []);
            }
        }

        public List<T> this[PanelType index]
        {
            get
            {
                return panels[index];
            }
            set
            {
                panels[index] = value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return panels.GetEnumerator();
        }

        IEnumerator<KeyValuePair<PanelType, List<T>>> IEnumerable<KeyValuePair<PanelType, List<T>>>.GetEnumerator()
        {
            return panels.GetEnumerator();
        }
    }
}
