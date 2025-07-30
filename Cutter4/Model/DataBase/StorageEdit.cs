using Cutter4.Model.Cutting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cutter4.Model.DataBase
{
    public class StorageEdit
    {
        public DateTime DateTime { get; }
        public int PanelLength { get; }
        public PanelColors PanelColor { get; }
        public string PanelWidth { get; }
        public int DeltaCount { get; }

        public StorageEdit(DateTime dateTime, int panelLength, PanelColors panelColor, string panelWidth, int deltaCount)
        {
            DateTime = dateTime;
            PanelLength = panelLength;
            PanelColor = panelColor;
            PanelWidth = panelWidth;
            DeltaCount = deltaCount;
        }
    }
}
