using PrettyLogcat.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PrettyLogcat.Services
{
    public class FilterService : IFilterService
    {
        private bool _showVerbose = true;
        private bool _showDebug = true;
        private bool _showInfo = true;
        private bool _showWarn = true;
        private bool _showError = true;
        private bool _showFatal = true;
        private string _tagFilter = string.Empty;
        private string _messageFilter = string.Empty;
        private string _pidFilter = string.Empty;
        
        // Search history (最多保存20个历史记录)
        private readonly List<string> _tagFilterHistory = new();
        private readonly List<string> _messageFilterHistory = new();
        private readonly List<string> _pidFilterHistory = new();
        private const int MaxHistoryCount = 20;

        public bool ShowVerbose
        {
            get => _showVerbose;
            set
            {
                if (_showVerbose != value)
                {
                    _showVerbose = value;
                    OnFiltersChanged();
                }
            }
        }

        public bool ShowDebug
        {
            get => _showDebug;
            set
            {
                if (_showDebug != value)
                {
                    _showDebug = value;
                    OnFiltersChanged();
                }
            }
        }

        public bool ShowInfo
        {
            get => _showInfo;
            set
            {
                if (_showInfo != value)
                {
                    _showInfo = value;
                    OnFiltersChanged();
                }
            }
        }

        public bool ShowWarn
        {
            get => _showWarn;
            set
            {
                if (_showWarn != value)
                {
                    _showWarn = value;
                    OnFiltersChanged();
                }
            }
        }

        public bool ShowError
        {
            get => _showError;
            set
            {
                if (_showError != value)
                {
                    _showError = value;
                    OnFiltersChanged();
                }
            }
        }

        public bool ShowFatal
        {
            get => _showFatal;
            set
            {
                if (_showFatal != value)
                {
                    _showFatal = value;
                    OnFiltersChanged();
                }
            }
        }

        public string TagFilter
        {
            get => _tagFilter;
            set
            {
                if (_tagFilter != value)
                {
                    var oldValue = _tagFilter;
                    _tagFilter = value ?? string.Empty;
                    
                    // 只有在非空值之间切换时才触发过滤更新
                    if (!string.IsNullOrWhiteSpace(oldValue) || !string.IsNullOrWhiteSpace(_tagFilter))
                    {
                        OnFiltersChanged();
                    }
                }
            }
        }

        public string MessageFilter
        {
            get => _messageFilter;
            set
            {
                if (_messageFilter != value)
                {
                    var oldValue = _messageFilter;
                    _messageFilter = value ?? string.Empty;
                    
                    // 只有在非空值之间切换时才触发过滤更新
                    if (!string.IsNullOrWhiteSpace(oldValue) || !string.IsNullOrWhiteSpace(_messageFilter))
                    {
                        OnFiltersChanged();
                    }
                }
            }
        }

        public string PidFilter
        {
            get => _pidFilter;
            set
            {
                if (_pidFilter != value)
                {
                    var oldValue = _pidFilter;
                    _pidFilter = value ?? string.Empty;
                    
                    // 只有在非空值之间切换时才触发过滤更新
                    if (!string.IsNullOrWhiteSpace(oldValue) || !string.IsNullOrWhiteSpace(_pidFilter))
                    {
                        OnFiltersChanged();
                    }
                }
            }
        }
        
        // Search history properties
        public IEnumerable<string> TagFilterHistory => _tagFilterHistory.AsReadOnly();
        public IEnumerable<string> MessageFilterHistory => _messageFilterHistory.AsReadOnly();
        public IEnumerable<string> PidFilterHistory => _pidFilterHistory.AsReadOnly();
        
        public void AddToTagHistory(string filter)
        {
            AddToHistory(_tagFilterHistory, filter);
        }
        
        public void AddToMessageHistory(string filter)
        {
            AddToHistory(_messageFilterHistory, filter);
        }
        
        public void AddToPidHistory(string filter)
        {
            AddToHistory(_pidFilterHistory, filter);
        }
        
        private void AddToHistory(List<string> history, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return;
                
            // Remove if already exists
            history.Remove(filter);
            
            // Add to beginning
            history.Insert(0, filter);
            
            // Keep only MaxHistoryCount items
            if (history.Count > MaxHistoryCount)
            {
                history.RemoveRange(MaxHistoryCount, history.Count - MaxHistoryCount);
            }
        }

        public event EventHandler? FiltersChanged;

        public bool ShouldIncludeLogEntry(LogEntry logEntry)
        {
            // Check log level filters
            var levelIncluded = logEntry.Level switch
            {
                Models.LogLevel.Verbose => ShowVerbose,
                Models.LogLevel.Debug => ShowDebug,
                Models.LogLevel.Info => ShowInfo,
                Models.LogLevel.Warn => ShowWarn,
                Models.LogLevel.Error => ShowError,
                Models.LogLevel.Fatal => ShowFatal,
                _ => true
            };

            if (!levelIncluded)
                return false;

            // Check tag filter
            if (!string.IsNullOrWhiteSpace(TagFilter))
            {
                if (!logEntry.Tag.Contains(TagFilter, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            // Check message filter
            if (!string.IsNullOrWhiteSpace(MessageFilter))
            {
                if (!logEntry.Message.Contains(MessageFilter, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            // Check PID filter
            if (!string.IsNullOrWhiteSpace(PidFilter))
            {
                if (int.TryParse(PidFilter, out var filterPid))
                {
                    if (logEntry.Pid != filterPid)
                        return false;
                }
                else
                {
                    // If PID filter is not a valid number, treat it as text search in PID string
                    if (!logEntry.Pid.ToString().Contains(PidFilter, StringComparison.OrdinalIgnoreCase))
                        return false;
                }
            }

            return true;
        }

        public IEnumerable<LogEntry> FilterLogEntries(IEnumerable<LogEntry> logEntries)
        {
            return logEntries.Where(ShouldIncludeLogEntry);
        }

        public void ResetFilters()
        {
            _showVerbose = true;
            _showDebug = true;
            _showInfo = true;
            _showWarn = true;
            _showError = true;
            _showFatal = true;
            _tagFilter = string.Empty;
            _messageFilter = string.Empty;
            _pidFilter = string.Empty;
            
            OnFiltersChanged();
        }

        private void OnFiltersChanged()
        {
            FiltersChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}