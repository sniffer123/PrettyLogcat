using PrettyLogcat.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace PrettyLogcat.Views
{
    public partial class PackageSelectionDialog : Window
    {
        public PidPackageInfo? SelectedPackage { get; private set; }
        
        private readonly List<PidPackageInfo> _allPackages;
        private readonly ObservableCollection<PidPackageInfo> _filteredPackages;

        public PackageSelectionDialog(IEnumerable<PidPackageInfo> packages)
        {
            InitializeComponent();
            
            _allPackages = packages.ToList();
            _filteredPackages = new ObservableCollection<PidPackageInfo>(_allPackages);
            
            PackageListBox.ItemsSource = _filteredPackages;
            
            // 聚焦到搜索框
            SearchTextBox.Focus();
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

        private void SearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var searchText = SearchTextBox.Text;
            FilterPackages(searchText);
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            SearchTextBox.Focus();
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    // 如果有选中的项目，确认选择
                    if (PackageListBox.SelectedItem is PidPackageInfo selected)
                    {
                        SelectedPackage = selected;
                        DialogResult = true;
                    }
                    else if (_filteredPackages.Count == 1)
                    {
                        // 如果只有一个结果，选择它
                        SelectedPackage = _filteredPackages[0];
                        DialogResult = true;
                    }
                    e.Handled = true;
                    break;
                    
                case Key.Escape:
                    // 清空搜索框
                    SearchTextBox.Text = string.Empty;
                    e.Handled = true;
                    break;
                    
                case Key.Down:
                    // 移动到列表
                    if (_filteredPackages.Count > 0)
                    {
                        PackageListBox.Focus();
                        if (PackageListBox.SelectedIndex < 0)
                        {
                            PackageListBox.SelectedIndex = 0;
                        }
                    }
                    e.Handled = true;
                    break;
            }
        }

        private void FilterPackages(string searchText)
        {
            _filteredPackages.Clear();
            
            if (string.IsNullOrWhiteSpace(searchText))
            {
                // 显示所有包，按包名排序
                foreach (var package in _allPackages.OrderBy(p => p.PackageName))
                {
                    _filteredPackages.Add(package);
                }
            }
            else
            {
                // 智能搜索：支持多种匹配方式
                var searchTerms = searchText.ToLowerInvariant().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
                
                var filtered = _allPackages.Where(package =>
                {
                    var packageNameLower = package.PackageName.ToLowerInvariant();
                    var pidString = package.Pid.ToString();
                    
                    // 检查是否所有搜索词都匹配
                    return searchTerms.All(term =>
                        packageNameLower.Contains(term) ||           // 包名包含搜索词
                        pidString.Contains(term) ||                  // PID包含搜索词
                        packageNameLower.Split('.').Any(part =>      // 包名的某个部分以搜索词开头
                            part.StartsWith(term)));
                })
                .OrderBy(package =>
                {
                    // 优先级排序：完全匹配 > 开头匹配 > 包含匹配
                    var packageNameLower = package.PackageName.ToLowerInvariant();
                    if (packageNameLower == searchText.ToLowerInvariant()) return 0;
                    if (packageNameLower.StartsWith(searchText.ToLowerInvariant())) return 1;
                    return 2;
                })
                .ThenBy(package => package.PackageName);
                
                foreach (var package in filtered)
                {
                    _filteredPackages.Add(package);
                }
            }
            
            // 如果只有一个结果，自动选中
            if (_filteredPackages.Count == 1)
            {
                PackageListBox.SelectedItem = _filteredPackages[0];
            }
            else if (_filteredPackages.Count > 0)
            {
                // 如果有多个结果，选中第一个
                PackageListBox.SelectedIndex = 0;
            }
        }
    }
}