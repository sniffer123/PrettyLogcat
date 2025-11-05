using System;
using PrettyLogcat.ViewModels;
using PrettyLogcat.Services;
using System.Windows;
using System.Windows.Controls;
using System.Linq;

namespace PrettyLogcat.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            if (viewModel == null)
                throw new ArgumentNullException(nameof(viewModel), "MainViewModel cannot be null.");

            InitializeComponent();
            DataContext = viewModel;
        }

        private void SelectPidByPackage_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                var availablePackages = viewModel.AvailablePidPackages.ToList();
                
                if (!availablePackages.Any())
                {
                    MessageBox.Show("No package information available. Connect to a device and view some logs first.", 
                                    "No Packages", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 创建选择对话框
                var dialog = new PackageSelectionDialog(availablePackages);
                if (dialog.ShowDialog() == true && dialog.SelectedPackage != null)
                {
                    // 设置选中的PID
                    viewModel.PidFilter = dialog.SelectedPackage.Pid.ToString();
                }
            }
        }
    }
}