using Microsoft.Extensions.Logging;
using PrettyLogcat.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PrettyLogcat.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly ILogger<SettingsService> _logger;
        private readonly string _settingsFilePath;
        private const int MaxHistoryItems = 8;
        
        private UserSettings _settings;

        public SettingsService(ILogger<SettingsService> logger)
        {
            _logger = logger;
            
            // Store settings in AppData\Local\PrettyLogcat
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appSettingsPath = Path.Combine(appDataPath, "PrettyLogcat");
            Directory.CreateDirectory(appSettingsPath);
            _settingsFilePath = Path.Combine(appSettingsPath, "settings.json");
            
            _settings = new UserSettings();
            LoadSettings();
        }

        // Column visibility settings
        public bool ShowTimeColumn
        {
            get => _settings.ShowTimeColumn;
            set { _settings.ShowTimeColumn = value; SaveSettings(); }
        }

        public bool ShowLevelColumn
        {
            get => _settings.ShowLevelColumn;
            set { _settings.ShowLevelColumn = value; SaveSettings(); }
        }

        public bool ShowPidColumn
        {
            get => _settings.ShowPidColumn;
            set { _settings.ShowPidColumn = value; SaveSettings(); }
        }

        public bool ShowTidColumn
        {
            get => _settings.ShowTidColumn;
            set { _settings.ShowTidColumn = value; SaveSettings(); }
        }

        public bool ShowTagColumn
        {
            get => _settings.ShowTagColumn;
            set { _settings.ShowTagColumn = value; SaveSettings(); }
        }

        public bool ShowMessageColumn
        {
            get => _settings.ShowMessageColumn;
            set { _settings.ShowMessageColumn = value; SaveSettings(); }
        }

        // Filter settings
        public bool ShowVerbose
        {
            get => _settings.ShowVerbose;
            set { _settings.ShowVerbose = value; SaveSettings(); }
        }

        public bool ShowDebug
        {
            get => _settings.ShowDebug;
            set { _settings.ShowDebug = value; SaveSettings(); }
        }

        public bool ShowInfo
        {
            get => _settings.ShowInfo;
            set { _settings.ShowInfo = value; SaveSettings(); }
        }

        public bool ShowWarn
        {
            get => _settings.ShowWarn;
            set { _settings.ShowWarn = value; SaveSettings(); }
        }

        public bool ShowError
        {
            get => _settings.ShowError;
            set { _settings.ShowError = value; SaveSettings(); }
        }

        public bool ShowFatal
        {
            get => _settings.ShowFatal;
            set { _settings.ShowFatal = value; SaveSettings(); }
        }

        // Filter values
        public string TagFilter
        {
            get => _settings.TagFilter;
            set { _settings.TagFilter = value; SaveSettings(); }
        }

        public string MessageFilter
        {
            get => _settings.MessageFilter;
            set { _settings.MessageFilter = value; SaveSettings(); }
        }

        public string PidFilter
        {
            get => _settings.PidFilter;
            set { _settings.PidFilter = value; SaveSettings(); }
        }

        // Filter history
        public List<string> TagFilterHistory
        {
            get => _settings.TagFilterHistory;
            set { _settings.TagFilterHistory = value; SaveSettings(); }
        }

        public List<string> MessageFilterHistory
        {
            get => _settings.MessageFilterHistory;
            set { _settings.MessageFilterHistory = value; SaveSettings(); }
        }

        public List<string> PidFilterHistory
        {
            get => _settings.PidFilterHistory;
            set { _settings.PidFilterHistory = value; SaveSettings(); }
        }

        public void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<UserSettings>(json);
                    if (settings != null)
                    {
                        _settings = settings;
                        _logger.LogInformation("Settings loaded from {FilePath}", _settingsFilePath);
                        return;
                    }
                }
                
                // Use default settings if file doesn't exist or is invalid
                _settings = new UserSettings();
                _logger.LogInformation("Using default settings");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load settings, using defaults");
                _settings = new UserSettings();
            }
        }

        public void SaveSettings()
        {
            try
            {
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(_settingsFilePath, json);
                _logger.LogDebug("Settings saved to {FilePath}", _settingsFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings");
            }
        }

        public void AddToFilterHistory(Models.FilterType filterType, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            List<string> history = filterType switch
            {
                Models.FilterType.Tag => _settings.TagFilterHistory,
                Models.FilterType.Message => _settings.MessageFilterHistory,
                Models.FilterType.Pid => _settings.PidFilterHistory,
                _ => throw new ArgumentOutOfRangeException(nameof(filterType))
            };

            // Remove existing entry if present
            history.Remove(value);
            
            // Add to beginning of list
            history.Insert(0, value);
            
            // Maintain max count
            while (history.Count > MaxHistoryItems)
            {
                history.RemoveAt(history.Count - 1);
            }

            SaveSettings();
        }

        private class UserSettings
        {
            // Column visibility - default TID to false
            public bool ShowTimeColumn { get; set; } = true;
            public bool ShowLevelColumn { get; set; } = true;
            public bool ShowPidColumn { get; set; } = true;
            public bool ShowTidColumn { get; set; } = false; // Default to hidden
            public bool ShowTagColumn { get; set; } = true;
            public bool ShowMessageColumn { get; set; } = true;

            // Filter settings - all log levels enabled by default
            public bool ShowVerbose { get; set; } = true;
            public bool ShowDebug { get; set; } = true;
            public bool ShowInfo { get; set; } = true;
            public bool ShowWarn { get; set; } = true;
            public bool ShowError { get; set; } = true;
            public bool ShowFatal { get; set; } = true;

            // Filter values
            public string TagFilter { get; set; } = string.Empty;
            public string MessageFilter { get; set; } = string.Empty;
            public string PidFilter { get; set; } = string.Empty;

            // Filter history
            public List<string> TagFilterHistory { get; set; } = new List<string>();
            public List<string> MessageFilterHistory { get; set; } = new List<string>();
            public List<string> PidFilterHistory { get; set; } = new List<string>();
        }
    }
}