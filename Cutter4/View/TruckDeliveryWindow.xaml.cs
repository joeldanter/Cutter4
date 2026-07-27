using Cutter4.ViewModel;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace Cutter4.View
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class TruckDeliveryWindow : Window
    {
        public TruckDeliveryWindow(TruckDeliveryViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }

        void Import_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
