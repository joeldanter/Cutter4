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
        List<Kapu> kapuk;
        PanelCollection<Configuration> confs;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            storage = new Storage();
            solver = new Solver();
            
            kapuk = new List<Kapu>()
            {
                new Kapu(2920, 2420, "F100", "Kapu1"),
                new Kapu(3700, 2950, "F100", "Kapu2"),
                new Kapu(5060, 2250, "F201", "Kapu3"),
                new Kapu(2860, 2960, "F100", "Kapu4"),
                new Kapu(5050, 2150, "F100", "Kapu5"),
                new Kapu(5070, 2200, "F301", "Kapu6")
            };
            confs = solver.Solve(kapuk, storage);
            Console.WriteLine(solver.GenerateOutput(kapuk, confs));
            Console.WriteLine();
            storage.BetterSave(kapuk, confs);
        }
    }
}
