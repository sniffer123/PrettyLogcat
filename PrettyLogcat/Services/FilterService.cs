using PrettyLogcat.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        
        // 搜索历史记录
        private readonly List<string> _messageFilterHistory = new();
        private readonly List<string> _tagFilterHistory = new();
        private readonly List<string> _pidFilterHistory = new();
        private const int MaxHistoryItems = 20;
        
        // PID到包名的映射缓存
        private readonly Dictionary<int, string> _pidToPackageMap = new();

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
                    var newValue = value ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(newValue))
                    {
                        AddToHistory(_tagFilterHistory, newValue);
                    }
                    _tagFilter = newValue;
                    OnFiltersChanged();
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
                    var newValue = value ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(newValue))
                    {
                        AddToHistory(_messageFilterHistory, newValue);
                    }
                    _messageFilter = newValue;
                    OnFiltersChanged();
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
                    var newValue = value ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(newValue))
                    {
                        AddToHistory(_pidFilterHistory, newValue);
                    }
                    _pidFilter = newValue;
                    OnFiltersChanged();
                }
            }
        }

        public event EventHandler? FiltersChanged;

        // 历史记录属性
        public IEnumerable<string> MessageFilterHistory => _messageFilterHistory.AsReadOnly();
        public IEnumerable<string> TagFilterHistory => _tagFilterHistory.AsReadOnly();
        public IEnumerable<string> PidFilterHistory => _pidFilterHistory.AsReadOnly();
        
        // PID包名选择项
        public IEnumerable<PidPackageInfo> AvailablePidPackages => GetAvailablePidPackages();

        /// <summary>
        /// Matches text against a filter expression with AND/OR logic.
        /// Default: spaces act as AND ("word1 word2" means both must be present)
        /// Explicit AND: "word1 AND word2", "word1 and word2", "word1 & word2", "word1 + word2" (both must be present)
        /// Explicit OR: "word1 OR word2", "word1 or word2", "word1 || word2" (at least one must be present)
        /// All operators are case-insensitive (AND/and/And, OR/or/Or all work)
        /// </summary>
        private bool MatchesFilter(string text, string filterExpression)
        {
            if (string.IsNullOrWhiteSpace(filterExpression))
                return true;

            // Split by OR operators (||, OR, or) - case insensitive
            var orGroups = System.Text.RegularExpressions.Regex.Split(
                filterExpression,
                @"\s*\|\|\s*|\s+(?:or)\s+",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            // Check if any OR group matches
            foreach (var orGroup in orGroups)
            {
                if (string.IsNullOrWhiteSpace(orGroup))
                    continue;

                // Split by AND operators (+, &, AND, and, or space) - case insensitive
                var andWords = System.Text.RegularExpressions.Regex.Split(
                    orGroup,
                    @"\s*\+\s*|\s+(?:&|and)\s+|\s+",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );

                // Check if all AND words are present
                var allAndWordsMatch = andWords
                    .Where(word => !string.IsNullOrWhiteSpace(word))
                    .All(word => text.Contains(word.Trim(), StringComparison.OrdinalIgnoreCase));

                if (allAndWordsMatch)
                    return true;
            }

            return false;
        }

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

            // Check tag filter with AND/OR logic
            if (!string.IsNullOrWhiteSpace(TagFilter))
            {
                if (!MatchesFilter(logEntry.Tag, TagFilter))
                    return false;
            }

            // Check message filter with AND/OR logic
            if (!string.IsNullOrWhiteSpace(MessageFilter))
            {
                if (!MatchesFilter(logEntry.Message, MessageFilter))
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
                    var pidString = logEntry.Pid.ToString();
                    if (!MatchesFilter(pidString, PidFilter))
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

        private void AddToHistory(List<string> history, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            // 移除如果已存在
            history.Remove(value);
            
            // 添加到开头
            history.Insert(0, value);
            
            // 限制历史记录数量
            if (history.Count > MaxHistoryItems)
            {
                history.RemoveAt(history.Count - 1);
            }
        }

        public void UpdatePidPackageMapping(int pid, string packageName)
        {
            if (!string.IsNullOrWhiteSpace(packageName))
            {
                _pidToPackageMap[pid] = packageName;
            }
        }

        private IEnumerable<PidPackageInfo> GetAvailablePidPackages()
        {
            return _pidToPackageMap
                .Select(kvp => new PidPackageInfo { Pid = kvp.Key, PackageName = kvp.Value })
                .OrderBy(p => p.PackageName)
                .ToList();
        }

        public void ClearHistory()
        {
            _messageFilterHistory.Clear();
            _tagFilterHistory.Clear();
            _pidFilterHistory.Clear();
        }
    }

    // PID包名信息类
    public class PidPackageInfo
    {
        public int Pid { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public string DisplayText => $"{PackageName} (PID: {Pid})";
    }
}