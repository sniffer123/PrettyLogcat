using Microsoft.Extensions.Logging;
using PrettyLogcat.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PrettyLogcat.Services
{
    public class AdbService : IAdbService, IDisposable
    {
        private readonly ILogger<AdbService> _logger;
        private readonly string _adbPath;
        private Process? _logcatProcess;
        private readonly Subject<string> _logcatSubject = new();

        public AdbService(ILogger<AdbService> logger)
        {
            _logger = logger;
            _adbPath = FindAdbPath();
        }

        public async Task<bool> IsAdbAvailableAsync()
        {
            try
            {
                var result = await ExecuteAdbCommandAsync("version");
                return !string.IsNullOrEmpty(result) && result.Contains("Android Debug Bridge");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check ADB availability");
                return false;
            }
        }

        public async Task<IEnumerable<AndroidDevice>> GetDevicesAsync()
        {
            try
            {
                var result = await ExecuteAdbCommandAsync("devices -l");
                return ParseDevicesOutput(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get devices");
                return Enumerable.Empty<AndroidDevice>();
            }
        }

        public async Task<AndroidDevice?> GetDeviceDetailsAsync(string deviceId)
        {
            try
            {
                var devices = await GetDevicesAsync();
                var device = devices.FirstOrDefault(d => d.Id == deviceId);
                
                if (device != null && device.IsOnline)
                {
                    // Get additional device properties
                    var model = await ExecuteDeviceCommandAsync(deviceId, "shell getprop ro.product.model");
                    var product = await ExecuteDeviceCommandAsync(deviceId, "shell getprop ro.product.name");
                    var deviceName = await ExecuteDeviceCommandAsync(deviceId, "shell getprop ro.product.device");

                    device.Model = model?.Trim() ?? string.Empty;
                    device.Product = product?.Trim() ?? string.Empty;
                    device.Device = deviceName?.Trim() ?? string.Empty;
                }

                return device;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get device details for {DeviceId}", deviceId);
                return null;
            }
        }

        public async Task<bool> ConnectToDeviceAsync(string deviceId)
        {
            try
            {
                var device = await GetDeviceDetailsAsync(deviceId);
                return device?.IsOnline == true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to device {DeviceId}", deviceId);
                return false;
            }
        }

        public Task<bool> DisconnectFromDeviceAsync(string deviceId)
        {
            try
            {
                StopLogcatStream();
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to disconnect from device {DeviceId}", deviceId);
                return Task.FromResult(false);
            }
        }

        public async Task ClearLogcatAsync(string deviceId)
        {
            try
            {
                await ExecuteDeviceCommandAsync(deviceId, "logcat -c");
                _logger.LogInformation("Cleared logcat for device {DeviceId}", deviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear logcat for device {DeviceId}", deviceId);
                throw;
            }
        }

        public IObservable<string> StartLogcatStream(string deviceId, CancellationToken cancellationToken)
        {
            StopLogcatStream();

            return Observable.Create<string>(observer =>
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _adbPath,
                        Arguments = $"-s {deviceId} logcat -v threadtime",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        observer.OnNext(e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        _logger.LogWarning("Logcat error: {Error}", e.Data);
                    }
                };

                try
                {
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    _logcatProcess = process;

                    _logger.LogInformation("Started logcat stream for device {DeviceId}", deviceId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to start logcat stream for device {DeviceId}", deviceId);
                    observer.OnError(ex);
                    return System.Reactive.Disposables.Disposable.Empty;
                }

                return System.Reactive.Disposables.Disposable.Create(() =>
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                        }
                        process.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error disposing logcat process");
                    }
                });
            });
        }

        public async Task<string> ExecuteAdbCommandAsync(string command, CancellationToken cancellationToken = default)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _adbPath,
                        Arguments = command,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                
                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
                {
                    throw new InvalidOperationException($"ADB command failed: {error}");
                }

                return output;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute ADB command: {Command}", command);
                throw;
            }
        }

        public async Task<string> ExecuteDeviceCommandAsync(string deviceId, string command, CancellationToken cancellationToken = default)
        {
            return await ExecuteAdbCommandAsync($"-s {deviceId} {command}", cancellationToken);
        }

        private void StopLogcatStream()
        {
            try
            {
                if (_logcatProcess != null && !_logcatProcess.HasExited)
                {
                    _logcatProcess.Kill();
                    _logcatProcess.Dispose();
                    _logcatProcess = null;
                    _logger.LogInformation("Stopped logcat stream");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping logcat stream");
            }
        }

        private string FindAdbPath()
        {
            // Try common ADB locations
            var commonPaths = new[]
            {
                @"C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe",
                @"C:\Android\Sdk\platform-tools\adb.exe",
                @"C:\Users\" + Environment.UserName + @"\AppData\Local\Android\Sdk\platform-tools\adb.exe",
                "adb.exe" // Try PATH
            };

            foreach (var path in commonPaths)
            {
                if (File.Exists(path) || path == "adb.exe")
                {
                    try
                    {
                        using var process = Process.Start(new ProcessStartInfo
                        {
                            FileName = path,
                            Arguments = "version",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        });
                        
                        if (process != null)
                        {
                            process.WaitForExit(5000);
                            if (process.ExitCode == 0)
                            {
                                return path;
                            }
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

            throw new FileNotFoundException("ADB not found. Please install Android SDK Platform Tools.");
        }

        private IEnumerable<AndroidDevice> ParseDevicesOutput(string output)
        {
            var devices = new List<AndroidDevice>();
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines.Skip(1)) // Skip "List of devices attached"
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                    continue;

                var deviceId = parts[0].Trim();
                var stateString = parts[1].Trim();

                if (!Enum.TryParse<DeviceState>(stateString, true, out var state))
                {
                    state = DeviceState.Unknown;
                }

                var device = new AndroidDevice
                {
                    Id = deviceId,
                    State = state
                };

                // Parse additional properties if available
                if (parts.Length > 2)
                {
                    var properties = parts[2];
                    var modelMatch = Regex.Match(properties, @"model:([^\s]+)");
                    var productMatch = Regex.Match(properties, @"product:([^\s]+)");
                    var deviceMatch = Regex.Match(properties, @"device:([^\s]+)");

                    if (modelMatch.Success)
                        device.Model = modelMatch.Groups[1].Value;
                    if (productMatch.Success)
                        device.Product = productMatch.Groups[1].Value;
                    if (deviceMatch.Success)
                        device.Device = deviceMatch.Groups[1].Value;
                }

                devices.Add(device);
            }

            return devices;
        }

        public void Dispose()
        {
            StopLogcatStream();
            _logcatSubject?.Dispose();
        }
    }
}