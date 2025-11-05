using PrettyLogcat.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrettyLogcat.Services
{
    public interface IFileService
    {
        Task<string?> OpenLogFileAsync();
        Task<bool> SaveLogFileAsync(IEnumerable<LogEntry> logEntries, string? filePath = null);
        Task<IEnumerable<LogEntry>> LoadLogFileAsync(string filePath);
        string GetDefaultLogFileName();
    }
}