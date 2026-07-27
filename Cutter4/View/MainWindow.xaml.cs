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
using Cutter4.Persistence;
using Cutter4.ViewModel;
using Microsoft.Extensions.DependencyInjection;

namespace Cutter4.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainViewModel viewModel;
        public MainWindow(MainViewModel viewModel)
        {
            this.viewModel = viewModel;
            DataContext = viewModel;
            InitializeComponent();
        }

        async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await viewModel.LoadDatabase();
        }

        void EditStorage_Click(object sender, RoutedEventArgs e)
        {
            EditStorageWindow editStorageWindow = App.Current.Services.GetRequiredService<EditStorageWindow>();
            editStorageWindow.ShowDialog();
        }
    }
}
