using Microsoft.Extensions.Logging;
using PrettyLogcat.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PrettyLogcat.Services
{
    public class DeviceService : IDeviceService, IDisposable
    {
        private readonly ILogger<DeviceService> _logger;
        private readonly IAdbService _adbService;
        private Timer? _deviceMonitorTimer;
        private List<AndroidDevice> _lastKnownDevices = new();
        private bool _isMonitoring;
        private bool _isRefreshing;

        public event EventHandler<DeviceEventArgs>? DeviceConnected;
        public event EventHandler<DeviceEventArgs>? DeviceDisconnected;
        public event EventHandler<IEnumerable<AndroidDevice>>? DevicesChanged;

        public DeviceService(ILogger<DeviceService> logger, IAdbService adbService)
        {
            _logger = logger;
            _adbService = adbService;
        }

        public async Task<IEnumerable<AndroidDevice>> GetDevicesAsync()
        {
            try
            {
                var devices = await _adbService.GetDevicesAsync();
                var deviceList = devices.ToList();

                // Get detailed information for each device
                var detailedDevices = new List<AndroidDevice>();
                foreach (var device in deviceList)
                {
                    var detailedDevice = await _adbService.GetDeviceDetailsAsync(device.Id);
                    detailedDevices.Add(detailedDevice ?? device);
                }

                return detailedDevices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get devices");
                return Enumerable.Empty<AndroidDevice>();
            }
        }

        public async Task<AndroidDevice?> GetDeviceAsync(string deviceId)
        {
            try
            {
                return await _adbService.GetDeviceDetailsAsync(deviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get device {DeviceId}", deviceId);
                return null;
            }
        }

        public async Task<bool> ConnectAsync(AndroidDevice device)
        {
            try
            {
                var success = await _adbService.ConnectToDeviceAsync(device.Id);
                if (success)
                {
                    _logger.LogInformation("Connected to device {DeviceId}", device.Id);
                    DeviceConnected?.Invoke(this, new DeviceEventArgs(device));
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to device {DeviceId}", device.Id);
                return false;
            }
        }

        public async Task<bool> DisconnectAsync(AndroidDevice device)
        {
            try
            {
                var success = await _adbService.DisconnectFromDeviceAsync(device.Id);
                if (success)
                {
                    _logger.LogInformation("Disconnected from device {DeviceId}", device.Id);
                    DeviceDisconnected?.Invoke(this, new DeviceEventArgs(device));
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to disconnect from device {DeviceId}", device.Id);
                return false;
            }
        }

        public async Task RefreshDevicesAsync()
        {
            if (_isRefreshing)
            {
                _logger.LogDebug("Device refresh already in progress, skipping");
                return;
            }

            try
            {
                _isRefreshing = true;
                _logger.LogDebug("Starting device refresh");

                var devices = await GetDevicesAsync();
                var deviceList = devices.ToList();

                // Compare with last known devices to detect changes
                var previousDeviceIds = _lastKnownDevices.Select(d => d.Id).ToHashSet();
                var currentDeviceIds = deviceList.Select(d => d.Id).ToHashSet();

                // Detect newly connected devices
                var newDevices = deviceList.Where(d => !previousDeviceIds.Contains(d.Id)).ToList();
                foreach (var device in newDevices)
                {
                    _logger.LogInformation("Device connected: {DeviceId}", device.Id);
                    DeviceConnected?.Invoke(this, new DeviceEventArgs(device));
                }

                // Detect disconnected devices
                var disconnectedDevices = _lastKnownDevices.Where(d => !currentDeviceIds.Contains(d.Id)).ToList();
                foreach (var device in disconnectedDevices)
                {
                    _logger.LogInformation("Device disconnected: {DeviceId}", device.Id);
                    DeviceDisconnected?.Invoke(this, new DeviceEventArgs(device));
                }

                _lastKnownDevices = deviceList;
                DevicesChanged?.Invoke(this, deviceList);
                
                _logger.LogDebug("Device refresh completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh devices");
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        public void StartDeviceMonitoring()
        {
            if (_isMonitoring)
                return;

            _isMonitoring = true;
            // 不再自动监控，只在手动刷新时更新设备列表
            _logger.LogInformation("Device monitoring initialized (manual refresh mode)");
        }

        public void StopDeviceMonitoring()
        {
            if (!_isMonitoring)
                return;

            _isMonitoring = false;
            _deviceMonitorTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _logger.LogInformation("Stopped device monitoring");
        }

        private async void MonitorDevicesCallback(object? state)
        {
            if (!_isMonitoring)
                return;

            try
            {
                await RefreshDevicesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during device monitoring");
            }
        }

        public void Dispose()
        {
            StopDeviceMonitoring();
            _deviceMonitorTimer?.Dispose();
        }
    }
}