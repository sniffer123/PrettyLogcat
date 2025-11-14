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
using System.Threading.Tasks;

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
        private Timer? _pendingLogTimer;
        private const int LogMergeDelayMs = 100; // 0.1秒延迟
        
        // 用于处理相同tid和时间戳的日志合并
        private LogEntry? _lastProcessedEntry;
        private readonly Dictionary<string, LogEntry> _tidTimestampCache = new();

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
                            FlushPendingLogEntry();
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
            
            // 清理待处理的日志条目和合并状态
            lock (_logProcessingLock)
            {
                FlushPendingLogEntry();
                _lastProcessedEntry = null;
                _tidTimestampCache.Clear();
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
                    var newLogEntry = ParseLogLine(line);
                    
                    // 检查是否可以与当前待处理的日志合并
                    if (_pendingLogEntry != null && CanMergeLogEntries(_pendingLogEntry, newLogEntry))
                    {
                        // 可以合并，将新日志合并到待处理的日志中
                        MergeLogEntries(_pendingLogEntry, newLogEntry);
                        
                        // 重新启动定时器，等待可能的更多合并
                        StartPendingLogTimer();
                    }
                    else
                    {
                        // 不能合并，先发送待处理的日志条目
                        FlushPendingLogEntry();
                        
                        // 设置新的待处理日志条目
                        _pendingLogEntry = newLogEntry;
                        
                        // 启动定时器，0.1秒后发送日志条目（如果没有续行的话）
                        StartPendingLogTimer();
                    }
                }
                else
                {
                    // 这是一个续行，添加到当前待处理的日志条目中
                    if (_pendingLogEntry != null)
                    {
                        // 取消定时器，因为有续行
                        CancelPendingLogTimer();
                        
                        // 将续行内容添加到消息中，保持换行格式
                        _pendingLogEntry.Message += Environment.NewLine + line.Trim();
                        _pendingLogEntry.RawLine += Environment.NewLine + line;
                        
                        // 重新启动定时器，等待可能的更多续行
                        StartPendingLogTimer();
                    }
                    else
                    {
                        // 如果没有待处理的日志条目，创建一个未知格式的条目并立即发送
                        var unknownEntry = new LogEntry
                        {
                            TimeStamp = DateTime.Now,
                            Level = Models.LogLevel.Info,
                            Pid = 0,
                            Tid = 0,
                            Tag = "Unknown",
                            Message = line.Trim(),
                            RawLine = line
                        };
                        _logEntriesSubject.OnNext(unknownEntry);
                    }
                }
            }
        }

        private void StartPendingLogTimer()
        {
            CancelPendingLogTimer();
            _pendingLogTimer = new Timer(OnPendingLogTimeout, null, LogMergeDelayMs, Timeout.Infinite);
        }

        private void CancelPendingLogTimer()
        {
            _pendingLogTimer?.Dispose();
            _pendingLogTimer = null;
        }

        private void OnPendingLogTimeout(object? state)
        {
            lock (_logProcessingLock)
            {
                FlushPendingLogEntry();
            }
        }

        private void FlushPendingLogEntry()
        {
            if (_pendingLogEntry != null)
            {
                // 发送待处理的日志条目
                _logEntriesSubject.OnNext(_pendingLogEntry);
                _lastProcessedEntry = _pendingLogEntry;
                _pendingLogEntry = null;
            }
            CancelPendingLogTimer();
        }

        private bool CanMergeLogEntries(LogEntry existingEntry, LogEntry newEntry)
        {
            // 检查是否为相同的pid、tid和时间戳
            return existingEntry.Pid == newEntry.Pid &&
                   existingEntry.Tid == newEntry.Tid && 
                   existingEntry.TimeStamp == newEntry.TimeStamp;
        }

        private void MergeLogEntries(LogEntry targetEntry, LogEntry sourceEntry)
        {
            // 将源日志的消息和原始行内容合并到目标日志中
            targetEntry.Message += Environment.NewLine + sourceEntry.Message;
            targetEntry.RawLine += Environment.NewLine + sourceEntry.RawLine;
            targetEntry.IsMerged = true;
            
            // 如果源日志被固定，目标日志也应该被固定
            if (sourceEntry.IsPinned)
            {
                targetEntry.IsPinned = true;
            }
        }

        private LogEntry? TryMergeWithPreviousLogEntry(LogEntry currentEntry)
        {
            // 如果没有上一条日志，无法合并
            if (_lastProcessedEntry == null)
                return null;

            // 检查是否为相同的pid、tid和时间戳
            if (CanMergeLogEntries(_lastProcessedEntry, currentEntry))
            {
                // 创建合并后的日志条目
                var mergedEntry = new LogEntry
                {
                    TimeStamp = _lastProcessedEntry.TimeStamp,
                    Level = _lastProcessedEntry.Level,
                    Pid = _lastProcessedEntry.Pid,
                    Tid = _lastProcessedEntry.Tid,
                    Tag = _lastProcessedEntry.Tag,
                    // 合并消息内容，保持换行格式
                    Message = _lastProcessedEntry.Message + Environment.NewLine + currentEntry.Message,
                    // 合并原始行内容
                    RawLine = _lastProcessedEntry.RawLine + Environment.NewLine + currentEntry.RawLine,
                    IsPinned = _lastProcessedEntry.IsPinned || currentEntry.IsPinned,
                    OriginalIndex = _lastProcessedEntry.OriginalIndex,
                    IsMerged = true
                };

                return mergedEntry;
            }

            return null;
        }

        public void ClearLogs()
        {
            lock (_logProcessingLock)
            {
                _lastProcessedEntry = null;
                _tidTimestampCache.Clear();
            }
            
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
            CancelPendingLogTimer();
            _logEntriesSubject?.Dispose();
        }
    }
}