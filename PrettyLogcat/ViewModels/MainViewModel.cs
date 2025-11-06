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
        private readonly ISettingsService _settingsService;

        private readonly ObservableCollection<AndroidDevice> _devices = new();
        private readonly ObservableCollection<LogEntry> _allLogs = new();
        private readonly ObservableCollection<LogEntry> _filteredLogs = new();
        private readonly ObservableCollection<LogEntry> _displayedLogs = new();
        private readonly ObservableCollection<LogEntry> _pinnedLogs = new();

        private AndroidDevice? _selectedDevice;
        private bool _isConnected;
        private bool _isLoading;
        private string _statusMessage = "Ready";
        private string _welcomeMessage = "Select a device and connect to start viewing logcat";
        private bool _showWelcomeMessage = true;
        private bool _autoScroll = true;
        private bool _wordWrap = false;
        private bool _showScrollToBottomButton = false;
        private string _secondarySearchText = string.Empty;
        private LogEntry? _selectedLogEntry;
        private CancellationTokenSource? _logcatCancellationTokenSource;
        private IDisposable? _logEntriesSubscription;

        // Column visibility properties - will be loaded from settings
        
        // 日志缓存和批量更新相关
        private readonly Queue<LogEntry> _logEntryCache = new();
        private readonly object _cacheLock = new object();
        private Timer? _uiUpdateTimer;
        private Timer? _pidPackageUpdateTimer;
        private bool _hasPendingUpdates = false;

        public ObservableCollection<AndroidDevice> Devices => _devices;
        public ObservableCollection<LogEntry> FilteredLogs => _filteredLogs;
        public ObservableCollection<LogEntry> DisplayedLogs => _displayedLogs;
        public ObservableCollection<LogEntry> PinnedLogs => _pinnedLogs;

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

        public bool ShowScrollToBottomButton
        {
            get => _showScrollToBottomButton;
            set
            {
                if (_showScrollToBottomButton != value)
                {
                    _showScrollToBottomButton = value;
                    OnPropertyChanged(nameof(ShowScrollToBottomButton));
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

        public string SecondarySearchText
        {
            get => _secondarySearchText;
            set
            {
                if (_secondarySearchText != value)
                {
                    _secondarySearchText = value;
                    OnPropertyChanged(nameof(SecondarySearchText));
                    ApplySecondarySearch();
                }
            }
        }

        public LogEntry? SelectedLogEntry
        {
            get => _selectedLogEntry;
            set
            {
                if (_selectedLogEntry != value)
                {
                    _selectedLogEntry = value;
                    OnPropertyChanged(nameof(SelectedLogEntry));
                }
            }
        }

        // Column visibility properties - use settings service
        public bool ShowTimeColumn
        {
            get => _settingsService.ShowTimeColumn;
            set
            {
                var oldValue = _settingsService.ShowTimeColumn;
                if (oldValue != value)
                {
                    _logger.LogDebug("ShowTimeColumn changing from {OldValue} to {NewValue}", oldValue, value);
                    _settingsService.ShowTimeColumn = value;
                    OnPropertyChanged(nameof(ShowTimeColumn));
                    _logger.LogDebug("ShowTimeColumn PropertyChanged triggered for value: {Value}", value);
                }
            }
        }

        public bool ShowLevelColumn
        {
            get => _settingsService.ShowLevelColumn;
            set
            {
                var oldValue = _settingsService.ShowLevelColumn;
                if (oldValue != value)
                {
                    _logger.LogDebug("ShowLevelColumn changing from {OldValue} to {NewValue}", oldValue, value);
                    _settingsService.ShowLevelColumn = value;
                    OnPropertyChanged(nameof(ShowLevelColumn));
                    _logger.LogDebug("ShowLevelColumn PropertyChanged triggered for value: {Value}", value);
                }
            }
        }

        public bool ShowPidColumn
        {
            get => _settingsService.ShowPidColumn;
            set
            {
                var oldValue = _settingsService.ShowPidColumn;
                if (oldValue != value)
                {
                    _logger.LogDebug("ShowPidColumn changing from {OldValue} to {NewValue}", oldValue, value);
                    _settingsService.ShowPidColumn = value;
                    OnPropertyChanged(nameof(ShowPidColumn));
                    _logger.LogDebug("ShowPidColumn PropertyChanged triggered for value: {Value}", value);
                }
            }
        }

        public bool ShowTidColumn
        {
            get => _settingsService.ShowTidColumn;
            set
            {
                var oldValue = _settingsService.ShowTidColumn;
                if (oldValue != value)
                {
                    _logger.LogDebug("ShowTidColumn changing from {OldValue} to {NewValue}", oldValue, value);
                    _settingsService.ShowTidColumn = value;
                    OnPropertyChanged(nameof(ShowTidColumn));
                    _logger.LogDebug("ShowTidColumn PropertyChanged triggered for value: {Value}", value);
                }
            }
        }

        public bool ShowTagColumn
        {
            get => _settingsService.ShowTagColumn;
            set
            {
                var oldValue = _settingsService.ShowTagColumn;
                if (oldValue != value)
                {
                    _logger.LogDebug("ShowTagColumn changing from {OldValue} to {NewValue}", oldValue, value);
                    _settingsService.ShowTagColumn = value;
                    OnPropertyChanged(nameof(ShowTagColumn));
                    _logger.LogDebug("ShowTagColumn PropertyChanged triggered for value: {Value}", value);
                }
            }
        }

        public bool ShowMessageColumn
        {
            get => _settingsService.ShowMessageColumn;
            set
            {
                var oldValue = _settingsService.ShowMessageColumn;
                if (oldValue != value)
                {
                    _logger.LogDebug("ShowMessageColumn changing from {OldValue} to {NewValue}", oldValue, value);
                    _settingsService.ShowMessageColumn = value;
                    OnPropertyChanged(nameof(ShowMessageColumn));
                    _logger.LogDebug("ShowMessageColumn PropertyChanged triggered for value: {Value}", value);
                }
            }
        }

        public string ConnectButtonText => IsConnected ? "Disconnect" : "Connect";
        public PackIconKind ConnectIconKind => IsConnected ? PackIconKind.LinkOff : PackIconKind.Link;
        public string ConnectionStatus => IsConnected ? "Connected" : "Disconnected";

        // Filter properties - use settings service and sync with filter service
        public bool ShowVerbose
        {
            get => _settingsService.ShowVerbose;
            set 
            { 
                if (_settingsService.ShowVerbose != value)
                {
                    _settingsService.ShowVerbose = value;
                    _filterService.ShowVerbose = value;
                    OnPropertyChanged(nameof(ShowVerbose));
                }
            }
        }

        public bool ShowDebug
        {
            get => _settingsService.ShowDebug;
            set 
            { 
                if (_settingsService.ShowDebug != value)
                {
                    _settingsService.ShowDebug = value;
                    _filterService.ShowDebug = value;
                    OnPropertyChanged(nameof(ShowDebug));
                }
            }
        }

        public bool ShowInfo
        {
            get => _settingsService.ShowInfo;
            set 
            { 
                if (_settingsService.ShowInfo != value)
                {
                    _settingsService.ShowInfo = value;
                    _filterService.ShowInfo = value;
                    OnPropertyChanged(nameof(ShowInfo));
                }
            }
        }

        public bool ShowWarn
        {
            get => _settingsService.ShowWarn;
            set 
            { 
                if (_settingsService.ShowWarn != value)
                {
                    _settingsService.ShowWarn = value;
                    _filterService.ShowWarn = value;
                    OnPropertyChanged(nameof(ShowWarn));
                }
            }
        }

        public bool ShowError
        {
            get => _settingsService.ShowError;
            set 
            { 
                if (_settingsService.ShowError != value)
                {
                    _settingsService.ShowError = value;
                    _filterService.ShowError = value;
                    OnPropertyChanged(nameof(ShowError));
                }
            }
        }

        public bool ShowFatal
        {
            get => _settingsService.ShowFatal;
            set 
            { 
                if (_settingsService.ShowFatal != value)
                {
                    _settingsService.ShowFatal = value;
                    _filterService.ShowFatal = value;
                    OnPropertyChanged(nameof(ShowFatal));
                }
            }
        }

        public string TagFilter
        {
            get => _settingsService.TagFilter;
            set 
            { 
                var oldValue = _settingsService.TagFilter;
                if (oldValue != value)
                {
                    _settingsService.TagFilter = value;
                    _filterService.TagFilter = value;
                    OnPropertyChanged(nameof(TagFilter));
                    
                    // Add to history when filter is applied (non-empty and different)
                    if (!string.IsNullOrWhiteSpace(value) && value != oldValue)
                    {
                        _settingsService.AddToFilterHistory(Models.FilterType.Tag, value);
                        OnPropertyChanged(nameof(TagFilterHistory));
                    }
                }
            }
        }

        public string MessageFilter
        {
            get => _settingsService.MessageFilter;
            set 
            { 
                var oldValue = _settingsService.MessageFilter;
                if (oldValue != value)
                {
                    _settingsService.MessageFilter = value;
                    _filterService.MessageFilter = value;
                    OnPropertyChanged(nameof(MessageFilter));
                    
                    // Add to history when filter is applied (non-empty and different)
                    if (!string.IsNullOrWhiteSpace(value) && value != oldValue)
                    {
                        _settingsService.AddToFilterHistory(Models.FilterType.Message, value);
                        OnPropertyChanged(nameof(MessageFilterHistory));
                    }
                }
            }
        }

        public string PidFilter
        {
            get => _settingsService.PidFilter;
            set 
            { 
                var oldValue = _settingsService.PidFilter;
                if (oldValue != value)
                {
                    _settingsService.PidFilter = value;
                    _filterService.PidFilter = value;
                    OnPropertyChanged(nameof(PidFilter));
                    
                    // Add to history when filter is applied (non-empty and different)
                    if (!string.IsNullOrWhiteSpace(value) && value != oldValue)
                    {
                        _settingsService.AddToFilterHistory(Models.FilterType.Pid, value);
                        OnPropertyChanged(nameof(PidFilterHistory));
                    }
                }
            }
        }

        public int TotalLogCount => _allLogs.Count;
        public int FilteredLogCount => _filteredLogs.Count;

        // 过滤器历史记录 - use settings service
        public IEnumerable<string> MessageFilterHistory => _settingsService.MessageFilterHistory;
        public IEnumerable<string> TagFilterHistory => _settingsService.TagFilterHistory;
        public IEnumerable<string> PidFilterHistory => _settingsService.PidFilterHistory;
        
        // PID包名选择
        public IEnumerable<PidPackageInfo> AvailablePidPackages => _filterService.AvailablePidPackages;

        // Commands
        public ICommand ConnectCommand { get; }
        public ICommand RefreshDevicesCommand { get; }
        public ICommand ClearLogsCommand { get; }
        public ICommand SaveLogsCommand { get; }
        public ICommand OpenFileCommand { get; }
        public ICommand CopyLogCommand { get; }
        public ICommand PinLogCommand { get; }
        public ICommand JumpToLogCommand { get; }
        public ICommand ClearSecondarySearchCommand { get; }
        public ICommand ScrollToBottomCommand { get; }

        public MainViewModel(
            ILogger<MainViewModel> logger,
            IDeviceService deviceService,
            ILogcatService logcatService,
            IFilterService filterService,
            IFileService fileService,
            IAdbService adbService,
            ISettingsService settingsService)
        {
            _logger = logger;
            _deviceService = deviceService;
            _logcatService = logcatService;
            _filterService = filterService;
            _fileService = fileService;
            _adbService = adbService;
            _settingsService = settingsService;

            // Initialize commands
            ConnectCommand = new RelayCommand(async () => await ExecuteConnectCommand(), CanExecuteConnect);
            RefreshDevicesCommand = new RelayCommand(async () => await ExecuteRefreshDevicesCommand(), CanExecuteRefreshDevices);
            ClearLogsCommand = new RelayCommand(ExecuteClearLogsCommand, () => _allLogs.Count > 0);
            SaveLogsCommand = new RelayCommand(async () => await ExecuteSaveLogsCommand(), () => _allLogs.Count > 0);
            OpenFileCommand = new RelayCommand(async () => await ExecuteOpenFileCommand());
            CopyLogCommand = new RelayCommand<LogEntry>(ExecuteCopyLogCommand);
            PinLogCommand = new RelayCommand<LogEntry>(ExecutePinLogCommand);
            JumpToLogCommand = new RelayCommand<LogEntry>(ExecuteJumpToLogCommand);
            ClearSecondarySearchCommand = new RelayCommand(ExecuteClearSecondarySearchCommand);
            ScrollToBottomCommand = new RelayCommand(ExecuteScrollToBottomCommand);

            // Subscribe to events
            _deviceService.DevicesChanged += OnDevicesChanged;
            _filterService.FiltersChanged += OnFiltersChanged;

            // Subscribe to log entries
            _logEntriesSubscription = _logcatService.LogEntries
                .Subscribe(OnLogEntryReceived);

            // Initialize UI update timer (200ms interval)
            _uiUpdateTimer = new Timer(ProcessLogCache, null, TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(200));
            
            // Initialize PID package update timer (5 seconds interval)
            _pidPackageUpdateTimer = new Timer(UpdatePidPackageUI, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

            // Load settings and sync with filter service
            LoadSettingsAndSync();

            // Initialize
            _ = Task.Run(InitializeAsync);
        }

        private void LoadSettingsAndSync()
        {
            try
            {
                // Settings are already loaded by SettingsService constructor
                // Sync settings to filter service
                _filterService.ShowVerbose = _settingsService.ShowVerbose;
                _filterService.ShowDebug = _settingsService.ShowDebug;
                _filterService.ShowInfo = _settingsService.ShowInfo;
                _filterService.ShowWarn = _settingsService.ShowWarn;
                _filterService.ShowError = _settingsService.ShowError;
                _filterService.ShowFatal = _settingsService.ShowFatal;
                
                _filterService.TagFilter = _settingsService.TagFilter;
                _filterService.MessageFilter = _settingsService.MessageFilter;
                _filterService.PidFilter = _settingsService.PidFilter;

                _logger.LogInformation("Settings loaded and synchronized with filter service");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load settings and sync with filter service");
            }
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

                // Initialize device service (manual refresh mode)
                _deviceService.StartDeviceMonitoring();
                
                // Initial device refresh
                await _deviceService.RefreshDevicesAsync();

                // Try to auto-connect to the first available device
                await TryAutoConnectAsync();

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

        private async Task TryAutoConnectAsync()
        {
            try
            {
                // Wait a moment for devices to be populated
                await Task.Delay(500);

                var availableDevice = _devices.FirstOrDefault(d => d.IsOnline);
                if (availableDevice != null)
                {
                    _logger.LogInformation("Auto-connecting to device: {DeviceId}", availableDevice.Id);
                    StatusMessage = $"Auto-connecting to {availableDevice.Name}...";
                    
                    // Set the selected device
                    SelectedDevice = availableDevice;
                    
                    // Try to connect
                    var success = await _deviceService.ConnectAsync(availableDevice);
                    if (success)
                    {
                        IsConnected = true;
                        ShowWelcomeMessage = false;
                        StatusMessage = $"Auto-connected to {availableDevice.Name}";

                        // Start logcat stream
                        _logcatCancellationTokenSource = new CancellationTokenSource();
                        _logcatService.StartLogcatStream(availableDevice.Id, _logcatCancellationTokenSource.Token);
                        
                        _logger.LogInformation("Auto-connected and started logcat stream for device {DeviceId}", availableDevice.Id);
                    }
                    else
                    {
                        StatusMessage = $"Failed to auto-connect to {availableDevice.Name}";
                        _logger.LogWarning("Auto-connect failed for device {DeviceId}", availableDevice.Id);
                    }
                }
                else
                {
                    StatusMessage = "No devices available for auto-connect";
                    _logger.LogInformation("No online devices found for auto-connect");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-connect to device");
                StatusMessage = "Auto-connect failed";
            }
        }

        private bool CanExecuteConnect()
        {
            return SelectedDevice != null && SelectedDevice.IsOnline && !IsLoading;
        }

        private bool CanExecuteRefreshDevices()
        {
            return !IsLoading;
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
                
                // 添加调试日志
                _logger.LogInformation("Started logcat stream for device {DeviceId}", SelectedDevice.Id);
            }
            else
            {
                StatusMessage = $"Failed to connect to {SelectedDevice.Name}";
                _logger.LogWarning("Failed to connect to device {DeviceId}", SelectedDevice?.Id);
            }
        }

        private async Task DisconnectFromDevice()
        {
            if (SelectedDevice == null)
                return;

            StatusMessage = $"Disconnecting from {SelectedDevice.Name}...";

            // Stop logcat stream safely
            try
            {
                if (_logcatCancellationTokenSource != null && !_logcatCancellationTokenSource.IsCancellationRequested)
                {
                    _logcatCancellationTokenSource.Cancel();
                }
            }
            catch (ObjectDisposedException)
            {
                _logger.LogDebug("CancellationTokenSource was already disposed during disconnect");
            }
            
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
                _displayedLogs.Clear();  // 添加清空显示的日志
                _pinnedLogs.Clear();     // 同时清空固定的日志
                
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
            var app = System.Windows.Application.Current;
            if (app != null)
            {
                app.Dispatcher.Invoke(() =>
                {
                    // 保存当前选中的设备ID
                    var selectedDeviceId = SelectedDevice?.Id;
                    
                    _devices.Clear();
                    foreach (var device in devices)
                    {
                        _devices.Add(device);
                    }

                    // 重新选择之前选中的设备（如果仍然存在）
                    if (!string.IsNullOrEmpty(selectedDeviceId))
                    {
                        var existingDevice = devices.FirstOrDefault(d => d.Id == selectedDeviceId);
                        if (existingDevice != null)
                        {
                            // 设备仍然存在，保持选中状态
                            SelectedDevice = existingDevice;
                        }
                        else
                        {
                            // 设备不再存在，清除选择并断开连接
                            SelectedDevice = null;
                            if (IsConnected)
                            {
                                _ = Task.Run(DisconnectFromDevice);
                            }
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
            
            // Notify UI of history changes
            OnPropertyChanged(nameof(MessageFilterHistory));
            OnPropertyChanged(nameof(TagFilterHistory));
            OnPropertyChanged(nameof(PidFilterHistory));
            OnPropertyChanged(nameof(AvailablePidPackages));
        }

        private void OnLogEntryReceived(LogEntry logEntry)
        {
            // 将日志条目添加到缓存中，所有处理都在后台线程
            lock (_cacheLock)
            {
                _logEntryCache.Enqueue(logEntry);
                _hasPendingUpdates = true;
            }
        }

        private void ProcessLogCache(object? state)
        {
            if (!_hasPendingUpdates)
                return;

            List<LogEntry> entriesToProcess;
            lock (_cacheLock)
            {
                if (_logEntryCache.Count == 0)
                {
                    _hasPendingUpdates = false;
                    return;
                }

                // 批量取出缓存中的日志条目
                entriesToProcess = new List<LogEntry>(_logEntryCache.Count);
                while (_logEntryCache.Count > 0)
                {
                    entriesToProcess.Add(_logEntryCache.Dequeue());
                }
                _hasPendingUpdates = false;
            }

            // 在后台线程处理数据，避免阻塞UI
            Task.Run(() =>
            {
                try
                {
                    // 创建临时集合来存储处理结果
                    var newAllLogs = new List<LogEntry>();
                    var newFilteredLogs = new List<LogEntry>();
                    var newDisplayedLogs = new List<LogEntry>();

                    // 在后台线程处理所有逻辑
                    foreach (var logEntry in entriesToProcess)
                    {
                        newAllLogs.Add(logEntry);
                        
                        // 尝试从Tag中提取包名信息并更新PID映射
                        TryExtractPackageNameFromLogEntry(logEntry);
                        
                        // 检查是否应该添加到过滤后的日志集合
                        if (_filterService.ShouldIncludeLogEntry(logEntry))
                        {
                            newFilteredLogs.Add(logEntry);
                            
                            // 应用二次搜索过滤
                            if (string.IsNullOrWhiteSpace(_secondarySearchText) ||
                                logEntry.Message.Contains(_secondarySearchText, StringComparison.OrdinalIgnoreCase) ||
                                logEntry.Tag.Contains(_secondarySearchText, StringComparison.OrdinalIgnoreCase))
                            {
                                newDisplayedLogs.Add(logEntry);
                            }
                        }
                    }

                    // 只在UI线程更新集合，最小化UI线程工作
                    var app = System.Windows.Application.Current;
                    if (app != null && newAllLogs.Count > 0)
                    {
                        app.Dispatcher.BeginInvoke(() =>
                        {
                            // 批量添加到UI集合
                            foreach (var entry in newAllLogs)
                            {
                                _allLogs.Add(entry);
                            }
                            
                            foreach (var entry in newFilteredLogs)
                            {
                                _filteredLogs.Add(entry);
                            }
                            
                            var hasNewDisplayedLogs = newDisplayedLogs.Count > 0;
                            foreach (var entry in newDisplayedLogs)
                            {
                                _displayedLogs.Add(entry);
                            }

                            // 更新统计信息
                            OnPropertyChanged(nameof(TotalLogCount));
                            OnPropertyChanged(nameof(FilteredLogCount));
                            
                            // 如果有新的显示日志且启用了自动滚动，触发滚动到底部
                            if (hasNewDisplayedLogs && AutoScroll)
                            {
                                ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing log cache");
                }
            });
        }

        private void ApplyFilters()
        {
            // 在后台线程处理过滤逻辑，避免阻塞UI
            Task.Run(() =>
            {
                try
                {
                    var allLogsCopy = new List<LogEntry>();
                    
                    // 获取所有日志的副本
                    var app = System.Windows.Application.Current;
                    if (app != null)
                    {
                        app.Dispatcher.Invoke(() =>
                        {
                            allLogsCopy.AddRange(_allLogs);
                        });
                    }

                    // 在后台线程应用过滤
                    var filtered = _filterService.FilterLogEntries(allLogsCopy);
                    var filteredList = new List<LogEntry>();
                    
                    // 为日志条目设置原始索引
                    int index = 0;
                    foreach (var entry in filtered)
                    {
                        entry.OriginalIndex = index++;
                        filteredList.Add(entry);
                    }

                    // 在UI线程更新过滤后的日志集合
                    if (app != null)
                    {
                        app.Dispatcher.BeginInvoke(() =>
                        {
                            _filteredLogs.Clear();
                            foreach (var entry in filteredList)
                            {
                                _filteredLogs.Add(entry);
                            }
                            OnPropertyChanged(nameof(FilteredLogCount));
                        });

                        // 应用二次搜索
                        ApplySecondarySearch();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error applying filters");
                }
            });
        }

        private void ApplySecondarySearch()
        {
            // 在后台线程处理搜索逻辑，避免阻塞UI
            Task.Run(() =>
            {
                try
                {
                    var searchText = _secondarySearchText;
                    var filteredLogsCopy = new List<LogEntry>();
                    
                    // 获取当前过滤后的日志副本
                    var app = System.Windows.Application.Current;
                    if (app != null)
                    {
                        app.Dispatcher.Invoke(() =>
                        {
                            filteredLogsCopy.AddRange(_filteredLogs);
                        });
                    }

                    // 在后台线程应用二次搜索
                    var displayedLogsCopy = new List<LogEntry>();
                    
                    if (string.IsNullOrWhiteSpace(searchText))
                    {
                        // 没有二次搜索条件，显示所有过滤后的日志
                        displayedLogsCopy.AddRange(filteredLogsCopy);
                    }
                    else
                    {
                        // 应用二次搜索
                        var searchTextLower = searchText.ToLowerInvariant();
                        foreach (var entry in filteredLogsCopy)
                        {
                            if (entry.Message.Contains(searchTextLower, StringComparison.OrdinalIgnoreCase) ||
                                entry.Tag.Contains(searchTextLower, StringComparison.OrdinalIgnoreCase))
                            {
                                displayedLogsCopy.Add(entry);
                            }
                        }
                    }

                    // 在UI线程更新显示的日志集合
                    if (app != null)
                    {
                        app.Dispatcher.BeginInvoke(() =>
                        {
                            _displayedLogs.Clear();
                            foreach (var entry in displayedLogsCopy)
                            {
                                _displayedLogs.Add(entry);
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error applying secondary search");
                }
            });
        }

        private void TryExtractPackageNameFromLogEntry(LogEntry logEntry)
        {
            // 尝试从Tag中提取包名信息
            // 常见的包名格式：com.example.app, 或者Tag中包含包名
            var tag = logEntry.Tag;
            
            // 检查Tag是否看起来像包名（包含点号且符合Java包名规范）
            if (!string.IsNullOrWhiteSpace(tag) && 
                tag.Contains('.') && 
                tag.Length > 3 &&
                !tag.Contains(' ') &&
                char.IsLetter(tag[0]))
            {
                // 可能是包名，更新映射（不立即通知UI更新，避免频繁刷新）
                _filterService.UpdatePidPackageMapping(logEntry.Pid, tag);
            }
            
            // 也可以尝试从Message中提取包名信息
            // 例如：ActivityManager相关的日志可能包含包名
            if (tag == "ActivityManager" || tag == "PackageManager")
            {
                var message = logEntry.Message;
                // 简单的包名提取逻辑
                var words = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    if (word.Contains('.') && 
                        word.Length > 3 && 
                        char.IsLetter(word[0]) &&
                        !word.Contains('/') &&
                        System.Text.RegularExpressions.Regex.IsMatch(word, @"^[a-zA-Z][a-zA-Z0-9_.]*[a-zA-Z0-9]$"))
                    {
                        _filterService.UpdatePidPackageMapping(logEntry.Pid, word);
                        break;
                    }
                }
            }
        }

        public async Task<IEnumerable<RunningPackageInfo>> GetRunningPackagesAsync()
        {
            if (SelectedDevice == null || !IsConnected)
            {
                return Enumerable.Empty<RunningPackageInfo>();
            }

            try
            {
                return await _adbService.GetRunningPackagesAsync(SelectedDevice.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get running packages");
                return Enumerable.Empty<RunningPackageInfo>();
            }
        }

        private void ExecuteCopyLogCommand(LogEntry? logEntry)
        {
            if (logEntry != null)
            {
                try
                {
                    var logText = $"{logEntry.TimeStamp:MM-dd HH:mm:ss.fff} {logEntry.Pid,5} {logEntry.Tid,5} {logEntry.Level} {logEntry.Tag}: {logEntry.Message}";
                    System.Windows.Clipboard.SetText(logText);
                    StatusMessage = "Log entry copied to clipboard";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to copy log entry to clipboard");
                    StatusMessage = "Failed to copy log entry";
                }
            }
        }

        private void ExecutePinLogCommand(LogEntry? logEntry)
        {
            if (logEntry != null)
            {
                try
                {
                    if (!logEntry.IsPinned)
                    {
                        logEntry.IsPinned = true;
                        _pinnedLogs.Add(logEntry);
                        StatusMessage = "Log entry pinned";
                    }
                    else
                    {
                        logEntry.IsPinned = false;
                        _pinnedLogs.Remove(logEntry);
                        StatusMessage = "Log entry unpinned";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to pin/unpin log entry");
                    StatusMessage = "Failed to pin/unpin log entry";
                }
            }
        }

        private void ExecuteJumpToLogCommand(LogEntry? logEntry)
        {
            if (logEntry != null)
            {
                try
                {
                    // 清空二次搜索
                    SecondarySearchText = string.Empty;
                    
                    // 在UI中选中该日志条目
                    SelectedLogEntry = logEntry;
                    
                    StatusMessage = $"Jumped to log entry at {logEntry.TimeStamp:HH:mm:ss.fff}";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to jump to log entry");
                    StatusMessage = "Failed to jump to log entry";
                }
            }
        }

        private void ExecuteClearSecondarySearchCommand()
        {
            SecondarySearchText = string.Empty;
            StatusMessage = "Secondary search cleared";
        }

        private void ExecuteScrollToBottomCommand()
        {
            try
            {
                // 触发滚动到底部事件（跳转到最后一条日志）
                ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);
                
                // 重新启用自动滚动
                AutoScroll = true;
                
                // 隐藏浮动按钮
                ShowScrollToBottomButton = false;
                
                StatusMessage = "Jumped to latest log, auto-scroll enabled";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to scroll to bottom");
                StatusMessage = "Failed to scroll to bottom";
            }
        }



        // 事件：请求滚动到底部
        public event EventHandler? ScrollToBottomRequested;

        private void UpdatePidPackageUI(object? state)
        {
            var app = System.Windows.Application.Current;
            if (app != null)
            {
                app.Dispatcher.BeginInvoke(() =>
                {
                    OnPropertyChanged(nameof(AvailablePidPackages));
                });
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var app = System.Windows.Application.Current;
            if (app != null)
            {
                if (app.Dispatcher.CheckAccess())
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                }
                else
                {
                    app.Dispatcher.BeginInvoke(() =>
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                    });
                }
            }
            else
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void Dispose()
        {
            try
            {
                // Save settings before cleanup
                _settingsService?.SaveSettings();
                _logger.LogInformation("Settings saved on application exit");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings on exit");
            }

            try
            {
                // Safely cancel and dispose CancellationTokenSource
                if (_logcatCancellationTokenSource != null && !_logcatCancellationTokenSource.IsCancellationRequested)
                {
                    _logcatCancellationTokenSource.Cancel();
                }
            }
            catch (ObjectDisposedException)
            {
                // CancellationTokenSource was already disposed, ignore
                _logger.LogDebug("CancellationTokenSource was already disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling CancellationTokenSource");
            }

            try
            {
                _logcatCancellationTokenSource?.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed, ignore
            }

            try
            {
                _logEntriesSubscription?.Dispose();
                _uiUpdateTimer?.Dispose();
                _pidPackageUpdateTimer?.Dispose();
                _deviceService?.StopDeviceMonitoring();
                
                if (_logcatService is IDisposable disposableLogcatService)
                    disposableLogcatService.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing resources");
            }
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

    // Generic RelayCommand implementation
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            if (parameter is T typedParameter)
            {
                return _canExecute?.Invoke(typedParameter) ?? true;
            }
            return _canExecute?.Invoke(default(T)) ?? true;
        }

        public void Execute(object? parameter)
        {
            if (parameter is T typedParameter)
            {
                _execute(typedParameter);
            }
            else
            {
                _execute(default(T));
            }
        }
    }
}