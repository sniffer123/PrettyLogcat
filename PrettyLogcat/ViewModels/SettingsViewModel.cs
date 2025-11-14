using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using PrettyLogcat.Services;
using PrettyLogcat.Models;

namespace PrettyLogcat.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly ISettingsService _settingsService;

        public SettingsViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            LoadFromSettings();
        }

        #region Properties

        // Column visibility settings
        private bool _showTimeColumn;
        public bool ShowTimeColumn
        {
            get => _showTimeColumn;
            set
            {
                _showTimeColumn = value;
                OnPropertyChanged(nameof(ShowTimeColumn));
            }
        }

        private bool _showLevelColumn;
        public bool ShowLevelColumn
        {
            get => _showLevelColumn;
            set
            {
                _showLevelColumn = value;
                OnPropertyChanged(nameof(ShowLevelColumn));
            }
        }

        private bool _showPidColumn;
        public bool ShowPidColumn
        {
            get => _showPidColumn;
            set
            {
                _showPidColumn = value;
                OnPropertyChanged(nameof(ShowPidColumn));
            }
        }

        private bool _showTidColumn;
        public bool ShowTidColumn
        {
            get => _showTidColumn;
            set
            {
                _showTidColumn = value;
                OnPropertyChanged(nameof(ShowTidColumn));
            }
        }

        private bool _showTagColumn;
        public bool ShowTagColumn
        {
            get => _showTagColumn;
            set
            {
                _showTagColumn = value;
                OnPropertyChanged(nameof(ShowTagColumn));
            }
        }

        private bool _showMessageColumn;
        public bool ShowMessageColumn
        {
            get => _showMessageColumn;
            set
            {
                _showMessageColumn = value;
                OnPropertyChanged(nameof(ShowMessageColumn));
            }
        }

        // Filter settings
        private bool _showVerbose;
        public bool ShowVerbose
        {
            get => _showVerbose;
            set
            {
                _showVerbose = value;
                OnPropertyChanged(nameof(ShowVerbose));
            }
        }

        private bool _showDebug;
        public bool ShowDebug
        {
            get => _showDebug;
            set
            {
                _showDebug = value;
                OnPropertyChanged(nameof(ShowDebug));
            }
        }

        private bool _showInfo;
        public bool ShowInfo
        {
            get => _showInfo;
            set
            {
                _showInfo = value;
                OnPropertyChanged(nameof(ShowInfo));
            }
        }

        private bool _showWarn;
        public bool ShowWarn
        {
            get => _showWarn;
            set
            {
                _showWarn = value;
                OnPropertyChanged(nameof(ShowWarn));
            }
        }

        private bool _showError;
        public bool ShowError
        {
            get => _showError;
            set
            {
                _showError = value;
                OnPropertyChanged(nameof(ShowError));
            }
        }

        private bool _showFatal;
        public bool ShowFatal
        {
            get => _showFatal;
            set
            {
                _showFatal = value;
                OnPropertyChanged(nameof(ShowFatal));
            }
        }

        // Display settings
        private int _logPreviewLineLimit;
        public int LogPreviewLineLimit
        {
            get => _logPreviewLineLimit;
            set
            {
                _logPreviewLineLimit = value;
                OnPropertyChanged(nameof(LogPreviewLineLimit));
            }
        }

        private bool _wordWrap;
        public bool WordWrap
        {
            get => _wordWrap;
            set
            {
                _wordWrap = value;
                OnPropertyChanged(nameof(WordWrap));
            }
        }

        private bool _autoScroll;
        public bool AutoScroll
        {
            get => _autoScroll;
            set
            {
                _autoScroll = value;
                OnPropertyChanged(nameof(AutoScroll));
            }
        }

        #endregion

        #region Methods

        private void LoadFromSettings()
        {
            // Column visibility
            ShowTimeColumn = _settingsService.ShowTimeColumn;
            ShowLevelColumn = _settingsService.ShowLevelColumn;
            ShowPidColumn = _settingsService.ShowPidColumn;
            ShowTidColumn = _settingsService.ShowTidColumn;
            ShowTagColumn = _settingsService.ShowTagColumn;
            ShowMessageColumn = _settingsService.ShowMessageColumn;

            // Filter settings
            ShowVerbose = _settingsService.ShowVerbose;
            ShowDebug = _settingsService.ShowDebug;
            ShowInfo = _settingsService.ShowInfo;
            ShowWarn = _settingsService.ShowWarn;
            ShowError = _settingsService.ShowError;
            ShowFatal = _settingsService.ShowFatal;

            // Display settings
            LogPreviewLineLimit = _settingsService.LogPreviewLineLimit;
            WordWrap = _settingsService.WordWrap;
            AutoScroll = _settingsService.AutoScroll;
        }

        public void ApplySettings()
        {
            // Column visibility
            _settingsService.ShowTimeColumn = ShowTimeColumn;
            _settingsService.ShowLevelColumn = ShowLevelColumn;
            _settingsService.ShowPidColumn = ShowPidColumn;
            _settingsService.ShowTidColumn = ShowTidColumn;
            _settingsService.ShowTagColumn = ShowTagColumn;
            _settingsService.ShowMessageColumn = ShowMessageColumn;

            // Filter settings
            _settingsService.ShowVerbose = ShowVerbose;
            _settingsService.ShowDebug = ShowDebug;
            _settingsService.ShowInfo = ShowInfo;
            _settingsService.ShowWarn = ShowWarn;
            _settingsService.ShowError = ShowError;
            _settingsService.ShowFatal = ShowFatal;

            // Display settings
            _settingsService.LogPreviewLineLimit = LogPreviewLineLimit;
            _settingsService.WordWrap = WordWrap;
            _settingsService.AutoScroll = AutoScroll;

            // Update LogEntry static property
            LogEntry.PreviewLineLimit = LogPreviewLineLimit;
        }

        public void ResetToDefaults()
        {
            // Column visibility - default TID to false
            ShowTimeColumn = true;
            ShowLevelColumn = true;
            ShowPidColumn = true;
            ShowTidColumn = false;
            ShowTagColumn = true;
            ShowMessageColumn = true;

            // Filter settings - all log levels enabled by default
            ShowVerbose = true;
            ShowDebug = true;
            ShowInfo = true;
            ShowWarn = true;
            ShowError = true;
            ShowFatal = true;

            // Display settings
            LogPreviewLineLimit = 3;
            WordWrap = false;
            AutoScroll = true;
        }

        public SettingsViewModel Clone()
        {
            var clone = new SettingsViewModel(_settingsService);
            clone.CopyFrom(this);
            return clone;
        }

        public void CopyFrom(SettingsViewModel other)
        {
            // Column visibility
            ShowTimeColumn = other.ShowTimeColumn;
            ShowLevelColumn = other.ShowLevelColumn;
            ShowPidColumn = other.ShowPidColumn;
            ShowTidColumn = other.ShowTidColumn;
            ShowTagColumn = other.ShowTagColumn;
            ShowMessageColumn = other.ShowMessageColumn;

            // Filter settings
            ShowVerbose = other.ShowVerbose;
            ShowDebug = other.ShowDebug;
            ShowInfo = other.ShowInfo;
            ShowWarn = other.ShowWarn;
            ShowError = other.ShowError;
            ShowFatal = other.ShowFatal;

            // Display settings
            LogPreviewLineLimit = other.LogPreviewLineLimit;
            WordWrap = other.WordWrap;
            AutoScroll = other.AutoScroll;
        }

        public void ClearTagFilterHistory()
        {
            _settingsService.TagFilterHistory.Clear();
            _settingsService.SaveSettings();
        }

        public void ClearMessageFilterHistory()
        {
            _settingsService.MessageFilterHistory.Clear();
            _settingsService.SaveSettings();
        }

        public void ClearPidFilterHistory()
        {
            _settingsService.PidFilterHistory.Clear();
            _settingsService.SaveSettings();
        }

        public void ClearQuickFilters()
        {
            _settingsService.QuickFilters.Clear();
            _settingsService.SaveSettings();
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}