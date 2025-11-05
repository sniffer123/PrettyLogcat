using PrettyLogcat.Models;
using System;
using System.Collections.Generic;

namespace PrettyLogcat.Services
{
    public interface IFilterService
    {
        bool ShowVerbose { get; set; }
        bool ShowDebug { get; set; }
        bool ShowInfo { get; set; }
        bool ShowWarn { get; set; }
        bool ShowError { get; set; }
        bool ShowFatal { get; set; }
        
        string TagFilter { get; set; }
        string MessageFilter { get; set; }
        string PidFilter { get; set; }
        
        // Search history
        IEnumerable<string> TagFilterHistory { get; }
        IEnumerable<string> MessageFilterHistory { get; }
        IEnumerable<string> PidFilterHistory { get; }
        
        void AddToTagHistory(string filter);
        void AddToMessageHistory(string filter);
        void AddToPidHistory(string filter);

        bool ShouldIncludeLogEntry(LogEntry logEntry);
        IEnumerable<LogEntry> FilterLogEntries(IEnumerable<LogEntry> logEntries);
        void ResetFilters();
        
        event EventHandler? FiltersChanged;
    }
}