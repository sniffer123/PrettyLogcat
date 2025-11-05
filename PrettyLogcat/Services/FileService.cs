using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using PrettyLogcat.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrettyLogcat.Services
{
    public class FileService : IFileService
    {
        private readonly ILogger<FileService> _logger;
        private readonly ILogcatService _logcatService;

        public FileService(ILogger<FileService> logger, ILogcatService logcatService)
        {
            _logger = logger;
            _logcatService = logcatService;
        }

        public Task<string?> OpenLogFileAsync()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Open Log File",
                    Filter = "Log Files (*.log;*.txt)|*.log;*.txt|All Files (*.*)|*.*",
                    DefaultExt = "log",
                    CheckFileExists = true,
                    CheckPathExists = true
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    return Task.FromResult<string?>(openFileDialog.FileName);
                }

                return Task.FromResult<string?>(null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open log file dialog");
                throw;
            }
        }

        public async Task<bool> SaveLogFileAsync(IEnumerable<LogEntry> logEntries, string? filePath = null)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    var saveFileDialog = new SaveFileDialog
                    {
                        Title = "Save Log File",
                        Filter = "Log Files (*.log)|*.log|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                        DefaultExt = "log",
                        FileName = GetDefaultLogFileName(),
                        AddExtension = true
                    };

                    if (saveFileDialog.ShowDialog() != true)
                    {
                        return false;
                    }

                    filePath = saveFileDialog.FileName;
                }

                var logContent = new StringBuilder();
                foreach (var entry in logEntries)
                {
                    logContent.AppendLine(entry.ToString());
                }

                await File.WriteAllTextAsync(filePath, logContent.ToString(), Encoding.UTF8);
                
                _logger.LogInformation("Saved {Count} log entries to {FilePath}", logEntries.Count(), filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save log file to {FilePath}", filePath);
                throw;
            }
        }

        public Task<IEnumerable<LogEntry>> LoadLogFileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Log file not found: {filePath}");
                }

                var logEntries = _logcatService.ParseLogFile(filePath);
                _logger.LogInformation("Loaded {Count} log entries from {FilePath}", logEntries.Count(), filePath);
                
                return Task.FromResult(logEntries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load log file from {FilePath}", filePath);
                throw;
            }
        }

        public string GetDefaultLogFileName()
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            return $"logcat_{timestamp}.log";
        }
    }
}