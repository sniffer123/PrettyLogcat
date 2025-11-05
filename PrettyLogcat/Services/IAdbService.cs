using PrettyLogcat.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PrettyLogcat.Services
{
    public interface IAdbService
    {
        Task<bool> IsAdbAvailableAsync();
        Task<IEnumerable<AndroidDevice>> GetDevicesAsync();
        Task<AndroidDevice?> GetDeviceDetailsAsync(string deviceId);
        Task<bool> ConnectToDeviceAsync(string deviceId);
        Task<bool> DisconnectFromDeviceAsync(string deviceId);
        Task ClearLogcatAsync(string deviceId);
        IObservable<string> StartLogcatStream(string deviceId, CancellationToken cancellationToken);
        Task<string> ExecuteAdbCommandAsync(string command, CancellationToken cancellationToken = default);
        Task<string> ExecuteDeviceCommandAsync(string deviceId, string command, CancellationToken cancellationToken = default);
        Task<IEnumerable<RunningPackageInfo>> GetRunningPackagesAsync(string deviceId);
    }
}