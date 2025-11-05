using Microsoft.Extensions.Logging;
using PrettyLogcat.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using System.Threading;

namespace PrettyLogcat.Services
{
    public class LogcatService : ILogcatService, IDisposable
    {
        private readonly ILogger<LogcatService> _logger;
        private readonly IAdbService _adbService;
        private readonly Subject<LogEntry> _logEntriesSubject = new();
        private IDisposable? _logcatSubscription;

        // Regex pattern for threadtime format: MM-dd HH:mm:ss.SSS PID TID LEVEL TAG: MESSAGE
        private static readonly Regex LogcatRegex = new(
            @"^(\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}\.\d{3})\s+(\d+)\s+(\d+)\s+([VDIWEF])\s+([^:]*?):\s*(.*?)$",
            RegexOptions.Compiled | RegexOptions.Multiline);

        public IObservable<LogEntry> LogEntries => _logEntriesSubject.AsObservable();

        public LogcatService(ILogger<LogcatService> logger, IAdbService adbService)
        {
            _logger = logger;
            _adbService = adbService;
        }

        public void StartLogcatStream(string deviceId, CancellationToken cancellationToken)
        {
            StopLogcatStream();

            _logcatSubscription = _adbService.StartLogcatStream(deviceId, cancellationToken)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(ParseLogLine)
                .Where(entry => entry != null)
                .Subscribe(
                    entry => _logEntriesSubject.OnNext(entry!),
                    error =>
                    {
                        _logger.LogError(error, "Error in logcat stream");
                        _logEntriesSubject.OnError(error);
                    },
                    () =>
                    {
                        _logger.LogInformation("Logcat stream completed");
                        _logEntriesSubject.OnCompleted();
                    });

            _logger.LogInformation("Started logcat service for device {DeviceId}", deviceId);
        }

        public void StopLogcatStream()
        {
            _logcatSubscription?.Dispose();
            _logcatSubscription = null;
            _logger.LogInformation("Stopped logcat service");
        }

        public void ClearLogs()
        {
            // This method is for clearing the UI logs, not the device logcat buffer
            // The actual device buffer clearing is handled by AdbService.ClearLogcatAsync
            _logger.LogInformation("Cleared local log entries");
        }

        public LogEntry? ParseLogLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            try
            {
                var match = LogcatRegex.Match(line);
                if (!match.Success)
                {
                    // Try to handle malformed lines or continuation lines
                    return new LogEntry
                    {
                        TimeStamp = DateTime.Now,
                        Level = Models.LogLevel.Info,
                        Pid = 0,
                        Tid = 0,
                        Tag = "Unknown",
                        Message = line.Trim(),
                        RawLine = line
                    };
                }

                var timeString = match.Groups[1].Value;
                var pidString = match.Groups[2].Value;
                var tidString = match.Groups[3].Value;
                var levelString = match.Groups[4].Value;
                var tag = match.Groups[5].Value.Trim();
                var message = match.Groups[6].Value;

                // Parse timestamp (add current year since logcat doesn't include it)
                var currentYear = DateTime.Now.Year;
                if (!DateTime.TryParseExact($"{currentYear}-{timeString}", 
                    "yyyy-MM-dd HH:mm:ss.fff", 
                    CultureInfo.InvariantCulture, 
                    DateTimeStyles.None, 
                    out var timestamp))
                {
                    timestamp = DateTime.Now;
                }

                // Parse PID and TID
                if (!int.TryParse(pidString, out var pid))
                    pid = 0;
                if (!int.TryParse(tidString, out var tid))
                    tid = 0;

                // Parse log level
                var logLevel = levelString.ToUpperInvariant() switch
                {
                    "V" => Models.LogLevel.Verbose,
                    "D" => Models.LogLevel.Debug,
                    "I" => Models.LogLevel.Info,
                    "W" => Models.LogLevel.Warn,
                    "E" => Models.LogLevel.Error,
                    "F" => Models.LogLevel.Fatal,
                    _ => Models.LogLevel.Info
                };

                return new LogEntry
                {
                    TimeStamp = timestamp,
                    Level = logLevel,
                    Pid = pid,
                    Tid = tid,
                    Tag = tag,
                    Message = message,
                    RawLine = line
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse log line: {Line}", line);
                return new LogEntry
                {
                    TimeStamp = DateTime.Now,
                    Level = Models.LogLevel.Info,
                    Pid = 0,
                    Tid = 0,
                    Tag = "ParseError",
                    Message = line,
                    RawLine = line
                };
            }
        }

        public IEnumerable<LogEntry> ParseLogFile(string filePath)
        {
            var entries = new List<LogEntry>();

            try
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var entry = ParseLogLine(line);
                    if (entry != null)
                    {
                        entries.Add(entry);
                    }
                }

                _logger.LogInformation("Parsed {Count} log entries from file {FilePath}", entries.Count, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse log file {FilePath}", filePath);
                throw;
            }

            return entries;
        }

        public void Dispose()
        {
            StopLogcatStream();
            _logEntriesSubject?.Dispose();
        }
    }
}