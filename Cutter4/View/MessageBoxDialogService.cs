using Cutter4.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace Cutter4.View
{
    internal class MessageBoxDialogService : IDialogService
    {
        public void ShowError(string message)
            => MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        public void ShowInfo(string message)
            => MessageBox.Show(message, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
