using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cutter4.Model.Cutting
{    
    public class Kapu
    {
        int length;
        public int Length
        {
            get
            {
                return length;
            }
            set
            {
                if (1 <= value && value <= MaxLength) length = value;
                else throw new ArgumentException("The length must be between 1 and 13200");
            }
        }
        int height;
        public int Height
        {
            get
            {
                return height;
            }
            set
            {
                if (1 <= value && value <= MaxHeight) height = value;
                else throw new ArgumentException("The height must be between 1 and 5210");
            }
        }
        public PanelColors Color { get; set; }
        public string SerialNumber { get; set; }

        static readonly int[,] KapuHeights =
        {
            // egy magassághoz tartozó 500mm és 610 mm panelok száma
            // 2160 mm kaput 3 500m esbol es 1 610mm esbol rakod ki
            { 550, 1, 0 },
            { 660, 0, 1 },
            { 1050, 2, 0 },
            { 1270, 0, 2 },
            { 1550, 3, 0 },
            { 1880, 0, 3 },
            { 2050, 4, 0 },
            { 2160, 3, 1 },
            { 2270, 2, 2 },
            { 2380, 1, 3 },
            { 2490, 0, 4 },
            { 2550, 5, 0 },
            { 2660, 4, 1 },
            { 2770, 3, 2 },
            { 2880, 2, 3 },
            { 2990, 1, 4 },
            { 3100, 0, 5 },
            { 3270, 4, 2 },
            { 3380, 3, 3 },
            { 3490, 2, 4 },
            { 3600, 1, 5 },
            { 3710, 0, 6 },
            { 3880, 4, 3 },
            { 3990, 3, 4 },
            { 4100, 2, 5 },
            { 4210, 1, 6 },
            { 4320, 0, 7 },
            { 4490, 4, 4 },
            { 4600, 3, 5 },
            { 4710, 2, 6 },
            { 4820, 1, 7 },
            { 4930, 0, 8 },
            { 5100, 4, 5 },
            { 5210, 3, 6 }
        };
        
        public const int MaxLength = 13200;
        public const int MaxHeight = 5210;

        public Kapu(int length, int height, PanelColors color, string serialNumber)
        {
            Length = length;
            Height = height;
            Color = color;
            SerialNumber = serialNumber;
        }

        // mennyi 500 as es mennyi 610 es panelbol lehet kirakni a kaput
        // lehetne ugy is hogy (int, int) PanelWidths() de ez igy egyertelmubb hogy mit ad ki (sztem legalabbis)
        public void GetPanelWidths(out int mm500, out int mm610)
        {
            for (int i = 0; i < KapuHeights.GetLength(0); i++)
            {
                if (Height <= KapuHeights[i, 0])
                {
                    mm500 = KapuHeights[i, 1];
                    mm610 = KapuHeights[i, 2];
                    return;
                }
            }

            throw new ArgumentException();
        }

        public static PanelCollection<KapuPanel> ProcessPanels(IList<Kapu> kapuk)
        {
            if (kapuk is null)
                throw new ArgumentNullException(nameof(kapuk) + " can not be null");

            // kulonbozo listakban tartalmazza a kulonbozo fajta (szin, 500mm vagy 610mm) panelokat
            // a listaban a kivagando panelok hossza es gyartasi szama van
            PanelCollection<KapuPanel> panels = new PanelCollection<KapuPanel>();
            foreach (Kapu kapu in kapuk)
            {
                kapu.GetPanelWidths(out int mm500, out int mm610);

                if (0 < mm500)
                {
                    PanelType type = new PanelType("500mm", kapu.Color);

                    for (int i = 0; i < mm500; i++)
                    {
                        panels[type].Add(new KapuPanel(kapu.Length, kapu.SerialNumber));
                    }
                }

                if (0 < mm610)
                {
                    PanelType type = new PanelType("610mm", kapu.Color);

                    for (int i = 0; i < mm610; i++)
                    {
                        panels[type].Add(new KapuPanel(kapu.Length, kapu.SerialNumber));
                    }
                }
            }

            return panels;
        }
    }
}
