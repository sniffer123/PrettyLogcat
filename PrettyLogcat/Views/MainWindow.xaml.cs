using System;
using PrettyLogcat.ViewModels;
using System.Windows;

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
    }
}