using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;
using PrettyLogcat.Models;
using PrettyLogcat.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PrettyLogcat.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly ILogger<MainViewModel> _logger;
        private readonly IDeviceService _deviceService;
        private readonly ILogcatService _logcatService;
        private readonly IFilterService _filterService;
        private readonly IFileService _fileService;
        private readonly IAdbService _adbService;

        private readonly ObservableCollection<AndroidDevice> _devices = new();
        private readonly ObservableCollection<LogEntry> _allLogs = new();
        private readonly ObservableCollection<LogEntry> _filteredLogs = new();

        private AndroidDevice? _selectedDevice;
        private bool _isConnected;
        private bool _isLoading;
        private string _statusMessage = "Ready";
        private string _welcomeMessage = "Select a device and connect to start viewing logcat";
        private bool _showWelcomeMessage = true;
        private bool _autoScroll = true;
        private bool _wordWrap = false;
        private CancellationTokenSource? _logcatCancellationTokenSource;
        private IDisposable? _logEntriesSubscription;

        public ObservableCollection<AndroidDevice> Devices => _devices;
        public ObservableCollection<LogEntry> FilteredLogs => _filteredLogs;

        public AndroidDevice? SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (_selectedDevice != value)
                {
                    _selectedDevice = value;
                    OnPropertyChanged(nameof(SelectedDevice));
                    OnPropertyChanged(nameof(ConnectButtonText));
                    OnPropertyChanged(nameof(ConnectIconKind));
                }
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnPropertyChanged(nameof(IsConnected));
                    OnPropertyChanged(nameof(ConnectButtonText));
                    OnPropertyChanged(nameof(ConnectIconKind));
                    OnPropertyChanged(nameof(ConnectionStatus));
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged(nameof(StatusMessage));
                }
            }
        }

        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set
            {
                if (_welcomeMessage != value)
                {
                    _welcomeMessage = value;
                    OnPropertyChanged(nameof(WelcomeMessage));
                }
            }
        }

        public bool ShowWelcomeMessage
        {
            get => _showWelcomeMessage;
            set
            {
                if (_showWelcomeMessage != value)
                {
                    _showWelcomeMessage = value;
                    OnPropertyChanged(nameof(ShowWelcomeMessage));
                }
            }
        }

        public bool AutoScroll
        {
            get => _autoScroll;
            set
            {
                if (_autoScroll != value)
                {
                    _autoScroll = value;
                    OnPropertyChanged(nameof(AutoScroll));
                }
            }
        }

        public bool WordWrap
        {
            get => _wordWrap;
            set
            {
                if (_wordWrap != value)
                {
                    _wordWrap = value;
                    OnPropertyChanged(nameof(WordWrap));
                }
            }
        }

        public string ConnectButtonText => IsConnected ? "Disconnect" : "Connect";
        public PackIconKind ConnectIconKind => IsConnected ? PackIconKind.LinkOff : PackIconKind.Link;
        public string ConnectionStatus => IsConnected ? "Connected" : "Disconnected";

        // Filter properties
        public bool ShowVerbose
        {
            get => _filterService.ShowVerbose;
            set => _filterService.ShowVerbose = value;
        }

        public bool ShowDebug
        {
            get => _filterService.ShowDebug;
            set => _filterService.ShowDebug = value;
        }

        public bool ShowInfo
        {
            get => _filterService.ShowInfo;
            set => _filterService.ShowInfo = value;
        }

        public bool ShowWarn
        {
            get => _filterService.ShowWarn;
            set => _filterService.ShowWarn = value;
        }

        public bool ShowError
        {
            get => _filterService.ShowError;
            set => _filterService.ShowError = value;
        }

        public bool ShowFatal
        {
            get => _filterService.ShowFatal;
            set => _filterService.ShowFatal = value;
        }

        public string TagFilter
        {
            get => _filterService.TagFilter;
            set => _filterService.TagFilter = value;
        }

        public string MessageFilter
        {
            get => _filterService.MessageFilter;
            set => _filterService.MessageFilter = value;
        }

        public string PidFilter
        {
            get => _filterService.PidFilter;
            set => _filterService.PidFilter = value;
        }

        public int TotalLogCount => _allLogs.Count;
        public int FilteredLogCount => _filteredLogs.Count;

        // Commands
        public ICommand ConnectCommand { get; }
        public ICommand RefreshDevicesCommand { get; }
        public ICommand ClearLogsCommand { get; }
        public ICommand SaveLogsCommand { get; }
        public ICommand OpenFileCommand { get; }

        public MainViewModel(
            ILogger<MainViewModel> logger,
            IDeviceService deviceService,
            ILogcatService logcatService,
            IFilterService filterService,
            IFileService fileService,
            IAdbService adbService)
        {
            _logger = logger;
            _deviceService = deviceService;
            _logcatService = logcatService;
            _filterService = filterService;
            _fileService = fileService;
            _adbService = adbService;

            // Initialize commands
            ConnectCommand = new RelayCommand(async () => await ExecuteConnectCommand(), CanExecuteConnect);
            RefreshDevicesCommand = new RelayCommand(async () => await ExecuteRefreshDevicesCommand());
            ClearLogsCommand = new RelayCommand(ExecuteClearLogsCommand, () => _allLogs.Count > 0);
            SaveLogsCommand = new RelayCommand(async () => await ExecuteSaveLogsCommand(), () => _allLogs.Count > 0);
            OpenFileCommand = new RelayCommand(async () => await ExecuteOpenFileCommand());

            // Subscribe to events
            _deviceService.DevicesChanged += OnDevicesChanged;
            _filterService.FiltersChanged += OnFiltersChanged;

            // Subscribe to log entries
            _logEntriesSubscription = _logcatService.LogEntries
                .Subscribe(OnLogEntryReceived);

            // Initialize
            _ = Task.Run(InitializeAsync);
        }

        private async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Initializing...";

                // Check ADB availability
                var adbAvailable = await _adbService.IsAdbAvailableAsync();
                if (!adbAvailable)
                {
                    StatusMessage = "ADB not found. Please install Android SDK Platform Tools.";
                    WelcomeMessage = "ADB not found. Please install Android SDK Platform Tools and restart the application.";
                    return;
                }

                // Start device monitoring
                _logger.LogInformation("Starting device monitoring...");
                _deviceService.StartDeviceMonitoring();
                
                // Initial device refresh
                _logger.LogInformation("Performing initial device refresh...");
                await _deviceService.RefreshDevicesAsync();
                _logger.LogInformation("Initial device refresh completed");

                StatusMessage = "Ready";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize application");
                StatusMessage = "Initialization failed";
                WelcomeMessage = "Failed to initialize. Please check your ADB installation.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanExecuteConnect()
        {
            return SelectedDevice != null && SelectedDevice.IsOnline;
        }

        private async Task ExecuteConnectCommand()
        {
            if (SelectedDevice == null)
                return;

            try
            {
                IsLoading = true;

                if (IsConnected)
                {
                    // Disconnect
                    await DisconnectFromDevice();
                }
                else
                {
                    // Connect
                    await ConnectToDevice();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute connect command");
                StatusMessage = $"Connection failed: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ConnectToDevice()
        {
            if (SelectedDevice == null)
                return;

            StatusMessage = $"Connecting to {SelectedDevice.Name}...";
            
            var success = await _deviceService.ConnectAsync(SelectedDevice);
            if (success)
            {
                IsConnected = true;
                ShowWelcomeMessage = false;
                StatusMessage = $"Connected to {SelectedDevice.Name}";

                // Start logcat stream
                _logcatCancellationTokenSource = new CancellationTokenSource();
                _logcatService.StartLogcatStream(SelectedDevice.Id, _logcatCancellationTokenSource.Token);
            }
            else
            {
                StatusMessage = $"Failed to connect to {SelectedDevice.Name}";
            }
        }

        private async Task DisconnectFromDevice()
        {
            if (SelectedDevice == null)
                return;

            StatusMessage = $"Disconnecting from {SelectedDevice.Name}...";

            // Stop logcat stream
            _logcatCancellationTokenSource?.Cancel();
            _logcatService.StopLogcatStream();

            var success = await _deviceService.DisconnectAsync(SelectedDevice);
            IsConnected = false;
            ShowWelcomeMessage = true;
            WelcomeMessage = "Select a device and connect to start viewing logcat";
            StatusMessage = success ? "Disconnected" : "Disconnect failed";
        }

        private async Task ExecuteRefreshDevicesCommand()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Refreshing devices...";
                await _deviceService.RefreshDevicesAsync();
                StatusMessage = "Devices refreshed";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh devices");
                StatusMessage = "Failed to refresh devices";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void ExecuteClearLogsCommand()
        {
            try
            {
                _allLogs.Clear();
                _filteredLogs.Clear();
                
                if (IsConnected && SelectedDevice != null)
                {
                    // Clear device logcat buffer
                    await _adbService.ClearLogcatAsync(SelectedDevice.Id);
                    StatusMessage = "Logs cleared";
                }
                else
                {
                    StatusMessage = "Local logs cleared";
                }

                OnPropertyChanged(nameof(TotalLogCount));
                OnPropertyChanged(nameof(FilteredLogCount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear logs");
                StatusMessage = "Failed to clear logs";
            }
        }

        private async Task ExecuteSaveLogsCommand()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Saving logs...";

                var success = await _fileService.SaveLogFileAsync(_filteredLogs);
                StatusMessage = success ? "Logs saved successfully" : "Save cancelled";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save logs");
                StatusMessage = "Failed to save logs";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExecuteOpenFileCommand()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Opening log file...";

                var filePath = await _fileService.OpenLogFileAsync();
                if (!string.IsNullOrEmpty(filePath))
                {
                    var logEntries = await _fileService.LoadLogFileAsync(filePath);
                    
                    _allLogs.Clear();
                    foreach (var entry in logEntries)
                    {
                        _allLogs.Add(entry);
                    }

                    ApplyFilters();
                    ShowWelcomeMessage = false;
                    StatusMessage = $"Loaded {logEntries.Count()} log entries from file";
                }
                else
                {
                    StatusMessage = "File open cancelled";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open log file");
                StatusMessage = "Failed to open log file";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnDevicesChanged(object? sender, IEnumerable<AndroidDevice> devices)
        {
            _logger.LogInformation("OnDevicesChanged called with {DeviceCount} devices", devices.Count());
            foreach (var device in devices)
            {
                _logger.LogInformation("Device: {DeviceId}, State: {State}, Model: {Model}, Name: {Name}", 
                    device.Id, device.State, device.Model, device.Name);
            }
            
            var app = System.Windows.Application.Current;
            if (app != null)
            {
                app.Dispatcher.Invoke(() =>
                {
                    _devices.Clear();
                    foreach (var device in devices)
                    {
                        _devices.Add(device);
                    }

                    // If selected device is no longer available, clear selection
                    if (SelectedDevice != null && !devices.Any(d => d.Id == SelectedDevice.Id))
                    {
                        SelectedDevice = null;
                        if (IsConnected)
                        {
                            _ = Task.Run(DisconnectFromDevice);
                        }
                    }
                });
            }
        }

        private void OnFiltersChanged(object? sender, EventArgs e)
        {
            ApplyFilters();
            
            // Notify UI of filter property changes
            OnPropertyChanged(nameof(ShowVerbose));
            OnPropertyChanged(nameof(ShowDebug));
            OnPropertyChanged(nameof(ShowInfo));
            OnPropertyChanged(nameof(ShowWarn));
            OnPropertyChanged(nameof(ShowError));
            OnPropertyChanged(nameof(ShowFatal));
            OnPropertyChanged(nameof(TagFilter));
            OnPropertyChanged(nameof(MessageFilter));
            OnPropertyChanged(nameof(PidFilter));
        }

        private void OnLogEntryReceived(LogEntry logEntry)
        {
            // Ensure UI updates happen on the UI thread
            var app = System.Windows.Application.Current;
            if (app != null)
            {
                app.Dispatcher.Invoke(() =>
                {
                    _allLogs.Add(logEntry);
                    
                    if (_filterService.ShouldIncludeLogEntry(logEntry))
                    {
                        _filteredLogs.Add(logEntry);
                    }

                    OnPropertyChanged(nameof(TotalLogCount));
                    OnPropertyChanged(nameof(FilteredLogCount));

                    // Auto-scroll if enabled
                    if (AutoScroll && _filteredLogs.Count > 0)
                    {
                        // Scroll to bottom logic would be implemented in the view
                    }
                });
            }
        }

        private void ApplyFilters()
        {
            var app = System.Windows.Application.Current;
            if (app != null)
            {
                app.Dispatcher.Invoke(() =>
                {
                    _filteredLogs.Clear();
                    var filtered = _filterService.FilterLogEntries(_allLogs);
                    foreach (var entry in filtered)
                    {
                        _filteredLogs.Add(entry);
                    }

                    OnPropertyChanged(nameof(FilteredLogCount));
                });
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            _logcatCancellationTokenSource?.Cancel();
            _logcatCancellationTokenSource?.Dispose();
            _logEntriesSubscription?.Dispose();
            _deviceService.StopDeviceMonitoring();
            if (_logcatService is IDisposable disposableLogcatService)
                disposableLogcatService.Dispose();
        }
    }

    // Simple RelayCommand implementation
    public class RelayCommand : ICommand
    {
        private readonly Func<Task>? _asyncExecute;
        private readonly Action? _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public RelayCommand(Func<Task> asyncExecute, Func<bool>? canExecute = null)
        {
            _asyncExecute = asyncExecute ?? throw new ArgumentNullException(nameof(asyncExecute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public async void Execute(object? parameter)
        {
            if (_asyncExecute != null)
            {
                await _asyncExecute();
            }
            else
            {
                _execute?.Invoke();
            }
        }
    }
}