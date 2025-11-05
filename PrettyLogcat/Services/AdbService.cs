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

                // 使用正则表达式来解析设备行，格式：deviceId + 多个空格 + state + 空格 + properties
                // 例如：emulator-5556          device product:aurora model:24031PN0DC device:aurora transport_id:1
                var match = Regex.Match(line.Trim(), @"^(\S+)\s+(\S+)(.*)$");
                if (!match.Success)
                {
                    _logger.LogWarning("Invalid device line format: '{Line}'", line);
                    continue;
                }

                var deviceId = match.Groups[1].Value.Trim();
                var stateString = match.Groups[2].Value.Trim();
                var propertiesString = match.Groups[3].Value.Trim();

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
                if (!string.IsNullOrWhiteSpace(propertiesString))
                {
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

        public async Task<IEnumerable<RunningPackageInfo>> GetRunningPackagesAsync(string deviceId)
        {
            try
            {
                _logger.LogDebug("Getting running packages for device {DeviceId}", deviceId);
                
                var psResult = await ExecuteDeviceCommandAsync(deviceId, "shell ps");
                var runningPackages = new List<RunningPackageInfo>();
                
                if (string.IsNullOrEmpty(psResult))
                {
                    _logger.LogWarning("No process information received from device {DeviceId}", deviceId);
                    return runningPackages;
                }
                
                var lines = psResult.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                var packagePidMap = new Dictionary<string, int>();
                
                foreach (var line in lines.Skip(1))
                {
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine)) continue;
                    
                    var parts = trimmedLine.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 9)
                    {
                        if (int.TryParse(parts[1], out var pid))
                        {
                            var processName = parts[8];
                            
                            if (processName.Contains('.') && !processName.StartsWith('[') && 
                                !processName.Contains("kernel") && !processName.Contains("kthreadd"))
                            {
                                packagePidMap[processName] = pid;
                            }
                        }
                    }
                }
                
                foreach (var kvp in packagePidMap)
                {
                    var packageInfo = new RunningPackageInfo
                    {
                        Pid = kvp.Value,
                        PackageName = kvp.Key,
                        ProcessName = kvp.Key
                    };
                    
                    runningPackages.Add(packageInfo);
                }
                
                _logger.LogInformation("Found {PackageCount} running packages on device {DeviceId}", 
                    runningPackages.Count, deviceId);
                
                return runningPackages.OrderBy(p => p.PackageName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get running packages for device {DeviceId}", deviceId);
                return Enumerable.Empty<RunningPackageInfo>();
            }
        }

        public void Dispose()
        {
            StopLogcatStream();
            _logcatSubject?.Dispose();
        }
    }
}