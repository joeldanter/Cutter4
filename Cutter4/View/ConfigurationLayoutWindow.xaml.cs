using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Cutter4.ViewModel;

namespace Cutter4.View
{
    public partial class ConfigurationLayoutWindow : Window
    {
        ConfigurationLayoutViewModel viewModel;
        private Point dragStart;

        public ConfigurationLayoutWindow(ConfigurationLayoutViewModel viewModel)
        {
            this.viewModel = viewModel;
            DataContext = viewModel;
            InitializeComponent();
        }

        void ConfItems_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => dragStart = e.GetPosition(null);

        void ConfItems_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            Point pos = e.GetPosition(null);
            if (Math.Abs(pos.X - dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(pos.Y - dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;

            if ((e?.OriginalSource as FrameworkElement)?.DataContext is ConfigurationItemViewModel item && !item.IsWaste)
            {
                ConfigurationViewModel? source = (sender as FrameworkElement)?.DataContext as ConfigurationViewModel;
                if (source == null) // so c# is happy with null checks
                    return;

                DataObject data = new();
                data.SetData(typeof(ConfigurationItemViewModel), item);
                data.SetData(typeof(ConfigurationViewModel), source);

                DragDrop.DoDragDrop((DependencyObject)sender, data, DragDropEffects.Move);
            }
        }

        void ConfItems_DragEnter(object sender, DragEventArgs e)
        {
            UpdateDragEffects(sender, e);
        }

        void ConfItems_DragOver(object sender, DragEventArgs e)
        {
            UpdateDragEffects(sender, e);
        }

        void ConfItems_DragLeave(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        void UpdateDragEffects(object sender, DragEventArgs e)
        {
            ConfigurationViewModel source = (ConfigurationViewModel)e.Data.GetData(typeof(ConfigurationViewModel));
            ConfigurationViewModel target = (ConfigurationViewModel)((FrameworkElement)sender).DataContext;

            e.Effects = source.Group == target.Group && source != target
                ? DragDropEffects.Move
                : DragDropEffects.None;
            e.Handled = true;
        }

        void ConfItems_Drop(object sender, DragEventArgs e)
        {
            ConfigurationItemViewModel item = (ConfigurationItemViewModel)e.Data.GetData(typeof(ConfigurationItemViewModel));
            ConfigurationViewModel source = (ConfigurationViewModel)e.Data.GetData(typeof(ConfigurationViewModel));
            ConfigurationViewModel target = (ConfigurationViewModel)((FrameworkElement)sender).DataContext;

            source.RemoveItem(item);
            target.AddItem(item.RealLength);
        }

        async void Apply_Click(object sender, RoutedEventArgs e)
        {
            if (await viewModel.Apply())
            {
                DialogResult = true;
                Close();
            }
        }

        void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
