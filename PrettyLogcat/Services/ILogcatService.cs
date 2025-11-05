using PrettyLogcat.Models;
using System;
using System.Collections.Generic;
using System.Threading;

namespace PrettyLogcat.Services
{
    public interface ILogcatService
    {
        IObservable<LogEntry> LogEntries { get; }
        void StartLogcatStream(string deviceId, CancellationToken cancellationToken);
        void StopLogcatStream();
        void ClearLogs();
        LogEntry? ParseLogLine(string line);
        IEnumerable<LogEntry> ParseLogFile(string filePath);
    }
}