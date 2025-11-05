using PrettyLogcat.Services;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace PrettyLogcat.Views
{
    public partial class PackageSelectionDialog : Window
    {
        public PidPackageInfo? SelectedPackage { get; private set; }

        public PackageSelectionDialog(IEnumerable<PidPackageInfo> packages)
        {
            InitializeComponent();
            DataContext = packages;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            SelectedPackage = PackageListBox.SelectedItem as PidPackageInfo;
            DialogResult = SelectedPackage != null;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is PidPackageInfo package)
            {
                SelectedPackage = package;
                DialogResult = true;
            }
        }
    }
}