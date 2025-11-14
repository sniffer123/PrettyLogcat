using System;
using PrettyLogcat.ViewModels;
using PrettyLogcat.Services;
using PrettyLogcat.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;

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
            
            // 订阅滚动到底部事件
            if (viewModel != null)
            {
                viewModel.ScrollToBottomRequested += OnScrollToBottomRequested;
            }
            
            // Initialize column visibility after the window is loaded
            this.Loaded += MainWindow_Loaded;
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

        private void PinnedLogItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row && row.DataContext is LogEntry logEntry)
            {
                if (DataContext is MainViewModel viewModel)
                {
                    // 跳转到该日志条目
                    viewModel.JumpToLogCommand.Execute(logEntry);
                    
                    // 在主DataGrid中选中该条目
                    var dataGrid = this.FindName("LogDataGrid") as DataGrid;
                    if (dataGrid?.ItemsSource != null)
                    {
                        dataGrid.SelectedItem = logEntry;
                        dataGrid.ScrollIntoView(logEntry);
                    }
                }
            }
        }

        private void ColumnHeader_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (sender is DataGridColumnHeader header && header.ContextMenu != null && DataContext is MainViewModel viewModel)
            {
                // 手动设置菜单项的状态和事件处理
                foreach (var item in header.ContextMenu.Items)
                {
                    if (item is MenuItem menuItem)
                    {
                        switch (menuItem.Header?.ToString())
                        {
                            case "Time":
                                menuItem.IsChecked = viewModel.ShowTimeColumn;
                                menuItem.Click -= TimeColumn_Click;
                                menuItem.Click += TimeColumn_Click;
                                break;
                            case "Level":
                                menuItem.IsChecked = viewModel.ShowLevelColumn;
                                menuItem.Click -= LevelColumn_Click;
                                menuItem.Click += LevelColumn_Click;
                                break;
                            case "PID":
                                menuItem.IsChecked = viewModel.ShowPidColumn;
                                menuItem.Click -= PidColumn_Click;
                                menuItem.Click += PidColumn_Click;
                                break;
                            case "TID":
                                menuItem.IsChecked = viewModel.ShowTidColumn;
                                menuItem.Click -= TidColumn_Click;
                                menuItem.Click += TidColumn_Click;
                                break;
                            case "Tag":
                                menuItem.IsChecked = viewModel.ShowTagColumn;
                                menuItem.Click -= TagColumn_Click;
                                menuItem.Click += TagColumn_Click;
                                break;
                            case "Message":
                                menuItem.IsChecked = viewModel.ShowMessageColumn;
                                menuItem.Click -= MessageColumn_Click;
                                menuItem.Click += MessageColumn_Click;
                                break;
                        }
                    }
                }
            }
        }

        private void TimeColumn_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                var oldValue = viewModel.ShowTimeColumn;
                viewModel.ShowTimeColumn = !viewModel.ShowTimeColumn;
                System.Diagnostics.Debug.WriteLine($"TimeColumn clicked: {oldValue} -> {viewModel.ShowTimeColumn}");
                
                // Force refresh DataGrid columns
                RefreshDataGridColumns();
            }
        }

        private void LevelColumn_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.ShowLevelColumn = !viewModel.ShowLevelColumn;
                System.Diagnostics.Debug.WriteLine($"LevelColumn clicked: -> {viewModel.ShowLevelColumn}");
                
                // Force refresh DataGrid columns
                RefreshDataGridColumns();
            }
        }

        private void PidColumn_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.ShowPidColumn = !viewModel.ShowPidColumn;
                System.Diagnostics.Debug.WriteLine($"PidColumn clicked: -> {viewModel.ShowPidColumn}");
                
                // Force refresh DataGrid columns
                RefreshDataGridColumns();
            }
        }

        private void TidColumn_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                var oldValue = viewModel.ShowTidColumn;
                viewModel.ShowTidColumn = !viewModel.ShowTidColumn;
                System.Diagnostics.Debug.WriteLine($"TidColumn clicked: {oldValue} -> {viewModel.ShowTidColumn}");
                
                // Force refresh DataGrid columns
                RefreshDataGridColumns();
            }
        }

        private void TagColumn_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.ShowTagColumn = !viewModel.ShowTagColumn;
                System.Diagnostics.Debug.WriteLine($"TagColumn clicked: -> {viewModel.ShowTagColumn}");
                
                // Force refresh DataGrid columns
                RefreshDataGridColumns();
            }
        }

        private void MessageColumn_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.ShowMessageColumn = !viewModel.ShowMessageColumn;
                System.Diagnostics.Debug.WriteLine($"MessageColumn clicked: -> {viewModel.ShowMessageColumn}");
                
                // Force refresh DataGrid columns
                RefreshDataGridColumns();
            }
        }

        private void LogDataGrid_ScrollChanged(object sender, System.Windows.Controls.ScrollChangedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                try
                {
                    var scrollViewer = GetScrollViewer(LogDataGrid);
                    if (scrollViewer != null)
                    {
                        // 检查是否滚动到底部（允许一些误差）
                        var isAtBottom = Math.Abs(scrollViewer.VerticalOffset - (scrollViewer.ScrollableHeight)) < 1.0;
                        
                        // 根据是否在底部决定浮动按钮的显示状态
                        if (isAtBottom)
                        {
                            // 在底部，隐藏浮动按钮，启用自动滚动
                            if (viewModel.ShowScrollToBottomButton)
                            {
                                viewModel.ShowScrollToBottomButton = false;
                            }
                            if (!viewModel.AutoScroll)
                            {
                                viewModel.AutoScroll = true;
                            }
                        }
                        else
                        {
                            // 不在底部，显示浮动按钮，禁用自动滚动
                            if (!viewModel.ShowScrollToBottomButton)
                            {
                                viewModel.ShowScrollToBottomButton = true;
                            }
                            if (viewModel.AutoScroll)
                            {
                                viewModel.AutoScroll = false;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in scroll changed: {ex.Message}");
                }
            }
        }

        private void OnScrollToBottomRequested(object? sender, EventArgs e)
        {
            try
            {
                ScrollToBottom();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error scrolling to bottom: {ex.Message}");
            }
        }

        private void ScrollToBottom()
        {
            try
            {
                var scrollViewer = GetScrollViewer(LogDataGrid);
                if (scrollViewer != null)
                {
                    // 确保滚动到最底部
                    scrollViewer.ScrollToEnd();
                    
                    // 如果有日志项目，也可以滚动到最后一项
                    if (LogDataGrid.Items.Count > 0)
                    {
                        var lastItem = LogDataGrid.Items[LogDataGrid.Items.Count - 1];
                        LogDataGrid.ScrollIntoView(lastItem);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ScrollToBottom: {ex.Message}");
            }
        }

        private System.Windows.Controls.ScrollViewer? GetScrollViewer(System.Windows.Controls.DataGrid dataGrid)
        {
            try
            {
                if (dataGrid == null) return null;

                var border = System.Windows.Media.VisualTreeHelper.GetChild(dataGrid, 0) as System.Windows.Controls.Decorator;
                if (border != null)
                {
                    var scrollViewer = border.Child as System.Windows.Controls.ScrollViewer;
                    return scrollViewer;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private void RefreshDataGridColumns()
        {
            try
            {
                if (DataContext is MainViewModel viewModel)
                {
                    // Refresh main DataGrid columns
                    if (LogDataGrid != null)
                    {
                        RefreshDataGridColumnsInternal(LogDataGrid, viewModel);
                        LogDataGrid.UpdateLayout();
                    }
                    
                    // Refresh pinned logs DataGrid columns
                    if (PinnedLogsDataGrid != null)
                    {
                        RefreshDataGridColumnsInternal(PinnedLogsDataGrid, viewModel);
                        PinnedLogsDataGrid.UpdateLayout();
                    }
                    
                    System.Diagnostics.Debug.WriteLine("DataGrid columns refreshed manually");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing DataGrid columns: {ex.Message}");
            }
        }

        private void RefreshDataGridColumnsInternal(DataGrid dataGrid, MainViewModel viewModel)
        {
            foreach (var column in dataGrid.Columns)
            {
                // For pinned logs, columns don't have headers, so we need to identify them by position or binding
                var headerText = column.Header?.ToString();
                
                // If no header (like in pinned logs), try to identify by binding or position
                if (string.IsNullOrEmpty(headerText))
                {
                    var columnIndex = dataGrid.Columns.IndexOf(column);
                    switch (columnIndex)
                    {
                        case 0: // Time column
                            column.Visibility = viewModel.ShowTimeColumn ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case 1: // Level column  
                            column.Visibility = viewModel.ShowLevelColumn ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case 2: // PID column
                            column.Visibility = viewModel.ShowPidColumn ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case 3: // TID column
                            column.Visibility = viewModel.ShowTidColumn ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case 4: // Tag column
                            column.Visibility = viewModel.ShowTagColumn ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case 5: // Message column
                            column.Visibility = viewModel.ShowMessageColumn ? Visibility.Visible : Visibility.Collapsed;
                            break;
                    }
                }
                else
                {
                    // Main DataGrid with headers
                    switch (headerText)
                    {
                        case "Time":
                            column.Visibility = viewModel.ShowTimeColumn ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case "Level":
                            column.Visibility = viewModel.ShowLevelColumn ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case "PID":
                            column.Visibility = viewModel.ShowPidColumn ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case "TID":
                            column.Visibility = viewModel.ShowTidColumn ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case "Tag":
                            column.Visibility = viewModel.ShowTagColumn ? Visibility.Visible : Visibility.Collapsed;
                            break;
                        case "Message":
                            column.Visibility = viewModel.ShowMessageColumn ? Visibility.Visible : Visibility.Collapsed;
                            break;
                    }
                }
            }
        }

        private void PinnedLogsToggle_Click(object sender, RoutedEventArgs e)
        {
            TogglePinnedLogs();
        }

        private void PinnedLogsHeader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TogglePinnedLogs();
        }

        private void TogglePinnedLogs()
        {
            try
            {
                var scrollViewer = PinnedLogsScrollViewer;
                var toggleIcon = PinnedLogsToggleIcon;
                
                if (scrollViewer != null && toggleIcon != null)
                {
                    if (scrollViewer.Visibility == Visibility.Visible)
                    {
                        // 收起
                        scrollViewer.Visibility = Visibility.Collapsed;
                        toggleIcon.Text = "▶";
                    }
                    else
                    {
                        // 展开
                        scrollViewer.Visibility = Visibility.Visible;
                        toggleIcon.Text = "▼";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error toggling pinned logs: {ex.Message}");
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize column visibility when window is loaded
            RefreshDataGridColumns();
        }

        protected override void OnClosed(EventArgs e)
        {
            // 取消订阅事件
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.ScrollToBottomRequested -= OnScrollToBottomRequested;
            }
            base.OnClosed(e);
        }

        #region Menu Event Handlers

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void RestartAdbMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.StatusMessage = "Restarting ADB server...";
                }

                // Kill ADB processes
                var killProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "taskkill",
                        Arguments = "/f /im adb.exe",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                killProcess.Start();
                await killProcess.WaitForExitAsync();

                // Start ADB server
                var startProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "adb",
                        Arguments = "start-server",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                startProcess.Start();
                await startProcess.WaitForExitAsync();

                if (DataContext is MainViewModel vm)
                {
                    vm.StatusMessage = "ADB server restarted successfully";
                    // Refresh devices after restart
                    if (vm.RefreshDevicesCommand.CanExecute(null))
                    {
                        vm.RefreshDevicesCommand.Execute(null);
                    }
                }

                MessageBox.Show("ADB server has been restarted successfully.", "ADB Restart", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.StatusMessage = $"Failed to restart ADB: {ex.Message}";
                }
                MessageBox.Show($"Failed to restart ADB server: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ConnectMuMuMenuItem_Click(object sender, RoutedEventArgs e)
        {
            await ConnectToEmulator("127.0.0.1:16416", "MuMu Emulator");
        }

        private async void ConnectLDPlayerMenuItem_Click(object sender, RoutedEventArgs e)
        {
            await ConnectToEmulator("127.0.0.1:5555", "LDPlayer");
        }

        private async Task ConnectToEmulator(string address, string emulatorName)
        {
            try
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.StatusMessage = $"Connecting to {emulatorName}...";
                }

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "adb",
                        Arguments = $"connect {address}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (DataContext is MainViewModel vm)
                {
                    vm.StatusMessage = $"Connected to {emulatorName}";
                    // Refresh devices after connection
                    if (vm.RefreshDevicesCommand.CanExecute(null))
                    {
                        vm.RefreshDevicesCommand.Execute(null);
                    }
                }

                MessageBox.Show($"Connection result: {output}", $"Connect to {emulatorName}", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.StatusMessage = $"Failed to connect to {emulatorName}: {ex.Message}";
                }
                MessageBox.Show($"Failed to connect to {emulatorName}: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void QuickScreenshotMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is MainViewModel viewModel)
                {
                    if (viewModel.SelectedDevice == null || !viewModel.IsConnected)
                    {
                        MessageBox.Show("Please connect to a device first.", "No Device Connected", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    viewModel.StatusMessage = "Taking screenshot...";
                }

                // Take screenshot using ADB
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "adb",
                        Arguments = "exec-out screencap -p",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                
                var imageData = new System.IO.MemoryStream();
                await process.StandardOutput.BaseStream.CopyToAsync(imageData);
                await process.WaitForExitAsync();

                if (imageData.Length > 0)
                {
                    // Convert to bitmap and copy to clipboard
                    imageData.Position = 0;
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = imageData;
                    bitmap.EndInit();
                    
                    Clipboard.SetImage(bitmap);
                    
                    if (DataContext is MainViewModel vm)
                    {
                        vm.StatusMessage = "Screenshot copied to clipboard";
                    }
                    
                    MessageBox.Show("Screenshot has been copied to clipboard!", "Screenshot Complete", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    throw new Exception("No screenshot data received");
                }
            }
            catch (Exception ex)
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.StatusMessage = $"Screenshot failed: {ex.Message}";
                }
                MessageBox.Show($"Failed to take screenshot: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var aboutMessage = @"PrettyLogcat - Android Logcat Viewer

Version: 1.0.0
Author: zkhuang

A modern, feature-rich Android logcat viewer with:
• Real-time log filtering and search
• Multiple device support
• Log level color coding
• Pin important logs
• Export and import functionality
• ADB integration tools

© 2024 All rights reserved.";

            MessageBox.Show(aboutMessage, "About PrettyLogcat", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PreferencesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is MainViewModel mainViewModel)
                {
                    var settingsWindow = mainViewModel.CreateSettingsWindow();
                    settingsWindow.Owner = this;
                    
                    if (settingsWindow.ShowDialog() == true)
                    {
                        // Settings were applied, refresh UI if needed
                        RefreshDataGridColumns();
                        
                        // Show confirmation
                        MessageBox.Show("Settings have been saved successfully.", "Settings Applied", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open settings: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UserGuideMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var guideMessage = @"PrettyLogcat User Guide

Getting Started:
1. Connect your Android device or start an emulator
2. Click 'Connect' to start viewing logs
3. Use filters to narrow down log entries

Key Features:
• Log Levels: Filter by Verbose, Debug, Info, Warning, Error, Fatal
• Text Filters: Search by message content, tag, or PID
• Pin Logs: Right-click any log entry to pin it for quick reference
• Column Visibility: Right-click column headers to show/hide columns
• Auto Scroll: Toggle automatic scrolling to latest logs
• Settings: Configure display options, line limits, and more

ADB Tools:
• Restart ADB Server: Fix connection issues
• Connect Emulators: Quick connect to MuMu/LDPlayer
• Quick Screenshot: Capture device screen to clipboard

Tips:
• Use Ctrl+C to copy selected log entries
• Double-click pinned logs to jump to them in main list
• Click long log entries to expand/collapse them
• Access Settings menu to customize display options
• Save logs to file for later analysis";

            MessageBox.Show(guideMessage, "User Guide", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Log Message Events

        private void LogMessage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock textBlock && textBlock.Tag is LogEntry logEntry)
            {
                if (DataContext is MainViewModel viewModel)
                {
                    // 只有多行日志才需要展开/收起
                    if (logEntry.IsMultiLine)
                    {
                        viewModel.ToggleLogExpandCommand.Execute(logEntry);
                    }
                }
            }
        }

        #endregion

        #region Quick Filter Events

        private void AddQuickFilter_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                var filterText = NewQuickFilterText.Text?.Trim();
                if (!string.IsNullOrEmpty(filterText))
                {
                    viewModel.AddQuickFilter(filterText);
                    NewQuickFilterText.Text = string.Empty;
                }
            }
        }

        private void RemoveQuickFilter_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel && sender is Button button)
            {
                if (button.CommandParameter is QuickFilter filter)
                {
                    viewModel.RemoveQuickFilter(filter);
                }
            }
        }

        private void QuickFilterItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel viewModel && sender is ListBoxItem item && item.DataContext is QuickFilter filter)
            {
                viewModel.ToggleQuickFilter(filter);
            }
        }

        private void NewQuickFilterText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is MainViewModel viewModel)
            {
                var filterText = NewQuickFilterText.Text?.Trim();
                if (!string.IsNullOrEmpty(filterText))
                {
                    viewModel.AddQuickFilter(filterText);
                    NewQuickFilterText.Text = string.Empty;
                }
            }
        }

        #endregion
    }
}