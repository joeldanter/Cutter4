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
using Cutter4.ViewModel;

namespace Cutter4.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainViewModel viewModel;
        public MainWindow()
        {
            InitializeComponent();
            viewModel = (MainViewModel)DataContext;
            if (!viewModel.TryLoadDatabase(out string errorMessage))
            {
                MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(-1);
            }
        }

        void EditStorage_Click(object sender, RoutedEventArgs e)
        {
            EditStorageWindow editStorageWindow = new();
            editStorageWindow.ShowDialog();
        }

        private void Revert_Click(object sender, RoutedEventArgs e)
        {
            viewModel.RevertOrder();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Save();
        }
    }
}
