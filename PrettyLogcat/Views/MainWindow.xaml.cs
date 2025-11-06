using System;
using PrettyLogcat.ViewModels;
using PrettyLogcat.Services;
using PrettyLogcat.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
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
    }
}