using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cutter4.Cutting
{
    public class PanelCollection<T> : IEnumerable<KeyValuePair<PanelType, List<T>>>
    {
        Dictionary<PanelType, List<T>> panels;

        public PanelCollection()
        {
            panels = new Dictionary<PanelType, List<T>>();

            foreach (PanelColors color in Enum.GetValues(typeof(PanelColors)))
            {
                panels.Add(new PanelType("500mm", color), new List<T>());
                panels.Add(new PanelType("610mm", color), new List<T>());
            }
        }

        public List<T> this[PanelType index]
        {
            get
            {
                return panels[index];
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
