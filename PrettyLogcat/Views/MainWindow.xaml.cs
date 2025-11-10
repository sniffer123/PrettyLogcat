using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using PrettyLogcat.ViewModels;

namespace PrettyLogcat.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow(MainViewModel viewModel)
        {
            if (viewModel == null)
                throw new ArgumentNullException(nameof(viewModel), "MainViewModel cannot be null.");

            InitializeComponent();
            DataContext = viewModel;
            _viewModel = viewModel;

            // Subscribe to collection changes for auto-scroll
            _viewModel.FilteredLogs.CollectionChanged += OnFilteredLogsChanged;
        }

        private void OnFilteredLogsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Only auto-scroll if enabled and items were added
            if (_viewModel.AutoScroll && e.Action == NotifyCollectionChangedAction.Add)
            {
                // Scroll to the bottom of the DataGrid
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (LogDataGrid.Items.Count > 0)
                    {
                        LogDataGrid.ScrollIntoView(LogDataGrid.Items[LogDataGrid.Items.Count - 1]);
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // Unsubscribe from events
            _viewModel.FilteredLogs.CollectionChanged -= OnFilteredLogsChanged;
            base.OnClosed(e);
        }
    }
}