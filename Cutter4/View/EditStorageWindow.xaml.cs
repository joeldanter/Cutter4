using Cutter4.ViewModel;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace Cutter4.View
{
    /// <summary>
    /// Interaction logic for StorageEditWindow.xaml
    /// </summary>
    public partial class EditStorageWindow : Window
    {
        EditStorageViewModel viewModel;
        public EditStorageWindow()
        {
            InitializeComponent();
            viewModel = (EditStorageViewModel)DataContext;
            MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
            viewModel.MainViewModel = (MainViewModel)mainWindow.DataContext;
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Apply();
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TruckDelivery_Click(object sender, RoutedEventArgs e)
        {
            TruckDeliveryWindow truckDeliveryWindow = new();

            if (truckDeliveryWindow.ShowDialog() == true)
            {
                TruckDeliveryViewModel truckDeliveryViewModel = (TruckDeliveryViewModel)truckDeliveryWindow.DataContext;
                viewModel.TruckDelivery(truckDeliveryViewModel.CountToImport);
            }
        }
    }
}
