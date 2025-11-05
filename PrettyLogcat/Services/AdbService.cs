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
            _logger.LogInformation("ADB Service initialized with path: {AdbPath}", _adbPath);
        }

        public async Task<bool> IsAdbAvailableAsync()
        {
            try
            {
                var result = await ExecuteAdbCommandAsync("version");
                var isAvailable = !string.IsNullOrEmpty(result) && result.Contains("Android Debug Bridge");
                _logger.LogInformation("ADB availability check: {IsAvailable}", isAvailable);
                return isAvailable;
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
                _logger.LogDebug("Executing 'adb devices -l' command");
                var result = await ExecuteAdbCommandAsync("devices -l");
                _logger.LogDebug("ADB devices output: {Output}", result);
                
                var devices = ParseDevicesOutput(result);
                _logger.LogInformation("Found {DeviceCount} devices", devices.Count());
                
                return devices;
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
                _logger.LogDebug("Executing ADB command: {Command}", command);
                
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

                _logger.LogDebug("ADB command output: {Output}", output);
                if (!string.IsNullOrEmpty(error))
                {
                    _logger.LogDebug("ADB command error: {Error}", error);
                }

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
                @"G:\Android\Sdk\platform-tools\adb.exe", // 根据实际检测到的路径
                @"C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe",
                @"C:\Android\Sdk\platform-tools\adb.exe",
                @"C:\Users\" + Environment.UserName + @"\AppData\Local\Android\Sdk\platform-tools\adb.exe",
                "adb" // Try PATH without .exe for cross-platform
            };

            foreach (var path in commonPaths)
            {
                try
                {
                    // 对于PATH中的adb，直接测试
                    if (path == "adb" || File.Exists(path))
                    {
                        using var process = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = path,
                                Arguments = "version",
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                CreateNoWindow = true
                            }
                        };
                        
                        process.Start();
                        var output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit(5000);
                        
                        if (process.ExitCode == 0 && output.Contains("Android Debug Bridge"))
                        {
                            _logger.LogInformation("Found ADB at: {AdbPath}", path);
                            return path;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Failed to test ADB path {Path}: {Error}", path, ex.Message);
                    continue;
                }
            }

            throw new FileNotFoundException("ADB not found. Please install Android SDK Platform Tools.");
        }

        private IEnumerable<AndroidDevice> ParseDevicesOutput(string output)
        {
            var devices = new List<AndroidDevice>();
            
            if (string.IsNullOrWhiteSpace(output))
            {
                _logger.LogWarning("ADB devices output is empty");
                return devices;
            }

            var lines = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            _logger.LogDebug("Parsing {LineCount} lines from ADB output", lines.Length);

            foreach (var line in lines.Skip(1)) // Skip "List of devices attached"
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                _logger.LogDebug("Parsing device line: '{Line}'", line);

                // 使用更灵活的分割方式，支持空格和制表符
                var parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                {
                    _logger.LogWarning("Invalid device line format: '{Line}'", line);
                    continue;
                }

                var deviceId = parts[0].Trim();
                var stateString = parts[1].Trim();

                // 映射设备状态
                DeviceState state = stateString.ToLowerInvariant() switch
                {
                    "device" => DeviceState.Device,
                    "offline" => DeviceState.Offline,
                    "unauthorized" => DeviceState.Unauthorized,
                    "bootloader" => DeviceState.Bootloader,
                    "recovery" => DeviceState.Recovery,
                    _ => DeviceState.Unknown
                };

                var device = new AndroidDevice
                {
                    Id = deviceId,
                    State = state
                };

                // Parse additional properties if available
                if (parts.Length > 2)
                {
                    var propertiesString = string.Join(" ", parts.Skip(2));
                    _logger.LogDebug("Device properties: '{Properties}'", propertiesString);

                    var modelMatch = Regex.Match(propertiesString, @"model:([^\s]+)");
                    var productMatch = Regex.Match(propertiesString, @"product:([^\s]+)");
                    var deviceMatch = Regex.Match(propertiesString, @"device:([^\s]+)");

                    if (modelMatch.Success)
                        device.Model = modelMatch.Groups[1].Value;
                    if (productMatch.Success)
                        device.Product = productMatch.Groups[1].Value;
                    if (deviceMatch.Success)
                        device.Device = deviceMatch.Groups[1].Value;
                }

                _logger.LogDebug("Parsed device: ID={DeviceId}, State={State}, Model={Model}", 
                    device.Id, device.State, device.Model);
                devices.Add(device);
            }

            _logger.LogInformation("Successfully parsed {DeviceCount} devices", devices.Count);
            return devices;
        }

        public void Dispose()
        {
            StopLogcatStream();
            _logcatSubject?.Dispose();
        }
    }
}