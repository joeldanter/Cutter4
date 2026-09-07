using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Cutter4.View
{
    public static class AutoCompleteBehavior
    {
        public static readonly DependencyProperty EnabledProperty =
            DependencyProperty.RegisterAttached(
                "Enabled",
                typeof(bool),
                typeof(AutoCompleteBehavior),
                new PropertyMetadata(false, OnEnabledChanged));

        public static void SetEnabled(DependencyObject element, bool value)
            => element.SetValue(EnabledProperty, value);
        public static bool GetEnabled(DependencyObject element)
            => (bool)element.GetValue(EnabledProperty);

        private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ComboBox comboBox)
                return;

            if ((bool)e.NewValue)
            {
                comboBox.IsEditable = true;
                comboBox.IsTextSearchEnabled = false; // we do our own filtering instead
                comboBox.StaysOpenOnEdit = true;
                comboBox.Loaded += ComboBox_Loaded;
            }
            else
            {
                comboBox.Loaded -= ComboBox_Loaded;
            }
        }

        private static void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            comboBox.Loaded -= ComboBox_Loaded;

            // Requires ItemsSource to already be set by the time the control loads.
            if (comboBox.Template.FindName("PART_EditableTextBox", comboBox) is not TextBox textBox)
                return;

            var view = CollectionViewSource.GetDefaultView(comboBox.ItemsSource);
            if (view == null) return;

            textBox.TextChanged += (_, _) =>
            {
                var text = textBox.Text;

                view.Filter = string.IsNullOrEmpty(text)
                    ? null
                    : o => o?.ToString()?.StartsWith(text, StringComparison.OrdinalIgnoreCase) == true;

                if (!comboBox.IsDropDownOpen && !string.IsNullOrEmpty(text))
                    comboBox.IsDropDownOpen = true;
            };

            // If the box is reopened right after picking an item, show the full list again
            // instead of the single-item filtered result left over from selecting it.
            comboBox.DropDownOpened += (_, _) =>
            {
                if (comboBox.SelectedItem != null &&
                    string.Equals(comboBox.Text, comboBox.SelectedItem.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    view.Filter = null;
                }
            };
        }
    }
}