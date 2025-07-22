using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Cutter4.Cutting;
using Cutter4.DataBase;
using MySql.Data.MySqlClient;

namespace Cutter4
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Storage storage;
        Solver solver;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            storage = new Storage();
            storage.Server = "127.0.0.1";
            storage.DatabaseName = "cutter";
            storage.UserName = "main";
            storage.Password = "Almacitrom"; // igen tudom nagyon biztonságos

            solver = new Solver();

            saveCut();
        }

        void saveCut()
        {
            List<Kapu> kapuk = new List<Kapu>()
            {
                new Kapu(2920, 2420, PanelColors.F100, "Kapu1"),
                new Kapu(3700, 2950, PanelColors.F100, "Kapu2"),
                new Kapu(5060, 2250, PanelColors.F201, "Kapu3"),
                new Kapu(2860, 2960, PanelColors.F100, "Kapu4"),
                new Kapu(5050, 2150, PanelColors.F100, "Kapu5"),
                new Kapu(5070, 2200, PanelColors.F301, "Kapu6")
            };

            PanelCollection<Cutting.KapuPanel> panelCollectionToCut = Kapu.ProcessPanels(kapuk);
            PanelCollection<Configuration> confs = solver.Solve(panelCollectionToCut, storage.StoragePanels());
            
            Console.WriteLine(solver.GenerateOutput(kapuk, confs));
            Console.WriteLine();
            //storage.SaveNewCut(kapuk, confs);

            showStorage();
        }

        void showStorage()
        {
            PanelCollection<StoragePanel> storagePanels = storage.StoragePanels();
            foreach (KeyValuePair<PanelType, List<StoragePanel>> entry in storagePanels)
            {
                PanelType type = entry.Key;
                List<StoragePanel> storagePanelsOfType = entry.Value;
                foreach (StoragePanel storagePanel in storagePanelsOfType)
                {
                    Console.WriteLine($"{type.Color} {type.Width} - {storagePanel.Count}x {storagePanel.Length}mm");
                }
                Console.WriteLine();
            }
        }
    }
}
