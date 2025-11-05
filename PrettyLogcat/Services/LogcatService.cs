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

        // 用于处理多行日志的缓存
        private LogEntry? _pendingLogEntry;
        private readonly object _logProcessingLock = new();

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
                .Subscribe(
                    line => ProcessLogLine(line),
                    error =>
                    {
                        _logger.LogError(error, "Error in logcat stream");
                        _logEntriesSubject.OnError(error);
                    },
                    () =>
                    {
                        // 处理最后一个待处理的日志条目
                        lock (_logProcessingLock)
                        {
                            if (_pendingLogEntry != null)
                            {
                                _logEntriesSubject.OnNext(_pendingLogEntry);
                                _pendingLogEntry = null;
                            }
                        }
                        _logger.LogInformation("Logcat stream completed");
                        _logEntriesSubject.OnCompleted();
                    });

            _logger.LogInformation("Started logcat service for device {DeviceId}", deviceId);
        }

        public void StopLogcatStream()
        {
            _logcatSubscription?.Dispose();
            _logcatSubscription = null;
            
            // 清理待处理的日志条目
            lock (_logProcessingLock)
            {
                if (_pendingLogEntry != null)
                {
                    _logEntriesSubject.OnNext(_pendingLogEntry);
                    _pendingLogEntry = null;
                }
            }
            
            _logger.LogInformation("Stopped logcat service");
        }

        private void ProcessLogLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            lock (_logProcessingLock)
            {
                var match = LogcatRegex.Match(line);
                
                if (match.Success)
                {
                    // 这是一个新的日志条目
                    // 如果有待处理的日志条目，先发送它
                    if (_pendingLogEntry != null)
                    {
                        _logEntriesSubject.OnNext(_pendingLogEntry);
                    }
                    
                    // 创建新的日志条目
                    _pendingLogEntry = ParseLogLine(line);
                }
                else
                {
                    // 这是一个续行，添加到当前待处理的日志条目中
                    if (_pendingLogEntry != null)
                    {
                        // 将续行内容添加到消息中，保持换行格式
                        _pendingLogEntry.Message += Environment.NewLine + line.Trim();
                        _pendingLogEntry.RawLine += Environment.NewLine + line;
                    }
                    else
                    {
                        // 如果没有待处理的日志条目，创建一个未知格式的条目
                        _pendingLogEntry = new LogEntry
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
                }
            }
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
                LogEntry? currentEntry = null;
                
                foreach (var line in lines)
                {
                    var match = LogcatRegex.Match(line);
                    
                    if (match.Success)
                    {
                        // 这是一个新的日志条目
                        if (currentEntry != null)
                        {
                            entries.Add(currentEntry);
                        }
                        
                        currentEntry = ParseLogLine(line);
                    }
                    else
                    {
                        // 这是一个续行
                        if (currentEntry != null && !string.IsNullOrWhiteSpace(line))
                        {
                            currentEntry.Message += Environment.NewLine + line.Trim();
                            currentEntry.RawLine += Environment.NewLine + line;
                        }
                    }
                }
                
                // 添加最后一个条目
                if (currentEntry != null)
                {
                    entries.Add(currentEntry);
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