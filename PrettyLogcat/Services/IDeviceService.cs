using PrettyLogcat.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrettyLogcat.Services
{
    public interface IDeviceService
    {
        event EventHandler<DeviceEventArgs>? DeviceConnected;
        event EventHandler<DeviceEventArgs>? DeviceDisconnected;
        event EventHandler<IEnumerable<AndroidDevice>>? DevicesChanged;

        Task<IEnumerable<AndroidDevice>> GetDevicesAsync();
        Task<AndroidDevice?> GetDeviceAsync(string deviceId);
        Task<bool> ConnectAsync(AndroidDevice device);
        Task<bool> DisconnectAsync(AndroidDevice device);
        Task RefreshDevicesAsync();
        void StartDeviceMonitoring();
        void StopDeviceMonitoring();
    }

    public class DeviceEventArgs : EventArgs
    {
        public AndroidDevice Device { get; }

        public DeviceEventArgs(AndroidDevice device)
        {
            Device = device;
        }
    }
}