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

        private async void SelectPidByPackage_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                try
                {
                    // 显示加载状态
                    var button = sender as Button;
                    var originalContent = button?.Content;
                    if (button != null)
                    {
                        button.Content = "⏳";
                        button.IsEnabled = false;
                    }

                    // 获取最新的运行包列表
                    var runningPackages = await viewModel.GetRunningPackagesAsync();
                    
                    if (!runningPackages.Any())
                    {
                        MessageBox.Show("No running packages found. Please make sure a device is connected and some apps are running.", 
                                        "No Running Packages", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // 转换为PidPackageInfo格式
                    var pidPackages = runningPackages.Select(rp => new PidPackageInfo
                    {
                        Pid = rp.Pid,
                        PackageName = rp.PackageName
                    }).ToList();

                    // 创建选择对话框
                    var dialog = new PackageSelectionDialog(pidPackages);
                    if (dialog.ShowDialog() == true && dialog.SelectedPackage != null)
                    {
                        // 设置选中的PID
                        viewModel.PidFilter = dialog.SelectedPackage.Pid.ToString();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to get running packages: {ex.Message}", 
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    // 恢复按钮状态
                    var button = sender as Button;
                    if (button != null)
                    {
                        button.Content = "📱";
                        button.IsEnabled = true;
                    }
                }
            }
        }
    }
}