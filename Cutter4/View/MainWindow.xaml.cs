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
        public MainWindow()
        {
            InitializeComponent();
            MainViewModel vm = (MainViewModel)DataContext;
            vm.SelectStoragePanelToAdd500Count += () => 
            {
                Keyboard.Focus(StoragePanelToAdd500Count);
                StoragePanelToAdd500Count.SelectAll();
            };
            vm.SelectStoragePanelToAdd610Count += () =>
            {
                Keyboard.Focus(StoragePanelToAdd610Count);
                StoragePanelToAdd610Count.SelectAll();
            };
        }
    }
}
