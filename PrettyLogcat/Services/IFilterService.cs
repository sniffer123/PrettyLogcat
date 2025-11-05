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

        bool ShouldIncludeLogEntry(LogEntry logEntry);
        IEnumerable<LogEntry> FilterLogEntries(IEnumerable<LogEntry> logEntries);
        void ResetFilters();
        
        event EventHandler? FiltersChanged;
    }
}