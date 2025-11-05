using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrettyLogcat.Services;
using PrettyLogcat.ViewModels;
using System;
using System.Windows;

namespace PrettyLogcat
{
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Set up global exception handling
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            DispatcherUnhandledException += OnDispatcherUnhandledException;

            try
            {
                base.OnStartup(e);

                // Configure services
                var services = new ServiceCollection();
                ConfigureServices(services);
                _serviceProvider = services.BuildServiceProvider();

                // Start main window
                var viewModel = _serviceProvider.GetRequiredService<MainViewModel>();
                var mainWindow = new Views.MainWindow(viewModel);
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                ShowError("Application startup failed", ex);
                Environment.Exit(1);
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Services
            services.AddSingleton<IAdbService, AdbService>();
            services.AddSingleton<IDeviceService, DeviceService>();
            services.AddSingleton<ILogcatService, LogcatService>();
            services.AddSingleton<IFilterService, FilterService>();
            services.AddSingleton<IFileService, FileService>();

            // ViewModels
            services.AddTransient<MainViewModel>();
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ShowError("Unhandled Exception", e.ExceptionObject as Exception);
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            ShowError("UI Thread Exception", e.Exception);
            e.Handled = true;
        }

        private void ShowError(string title, Exception? ex)
        {
            var message = ex?.Message ?? "Unknown error";
            var details = ex?.ToString() ?? "No details available";
            var fullMessage = message + "\n\nDetails:\n" + details;
            
            System.Windows.MessageBox.Show(fullMessage, 
                "PrettyLogcat - " + title, 
                MessageBoxButton.OK, 
                MessageBoxImage.Error);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}