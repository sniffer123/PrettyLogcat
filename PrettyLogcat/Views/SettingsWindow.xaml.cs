using System;
using System.Windows;
using PrettyLogcat.ViewModels;

namespace PrettyLogcat.Views
{
    public partial class SettingsWindow : Window
    {
        private SettingsViewModel _viewModel;
        private SettingsViewModel _originalSettings;

        public SettingsWindow(SettingsViewModel viewModel)
        {
            if (viewModel == null)
                throw new ArgumentNullException(nameof(viewModel));

            InitializeComponent();
            
            // Create a copy for potential rollback
            _originalSettings = viewModel.Clone();
            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            // Apply settings
            _viewModel.ApplySettings();
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // Restore original settings
            _viewModel.CopyFrom(_originalSettings);
            DialogResult = false;
            Close();
        }

        private void ResetToDefaults_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to reset all settings to their default values?",
                "Reset to Defaults",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _viewModel.ResetToDefaults();
            }
        }

        private void ClearTagFilterHistory_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ClearTagFilterHistory();
            MessageBox.Show("Tag filter history has been cleared.", "History Cleared", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearMessageFilterHistory_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ClearMessageFilterHistory();
            MessageBox.Show("Message filter history has been cleared.", "History Cleared", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearPidFilterHistory_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ClearPidFilterHistory();
            MessageBox.Show("PID filter history has been cleared.", "History Cleared", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearQuickFilters_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to clear all quick filters?",
                "Clear Quick Filters",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _viewModel.ClearQuickFilters();
                MessageBox.Show("All quick filters have been cleared.", "Quick Filters Cleared", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}