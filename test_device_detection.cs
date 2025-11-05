using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PrettyLogcat.Services;

class Program
{
    static async Task Main(string[] args)
    {
        // 创建简单的控制台日志记录器
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole().SetMinimumLevel(LogLevel.Debug);
        });
        
        var logger = loggerFactory.CreateLogger<AdbService>();
        
        try
        {
            var adbService = new AdbService(logger);
            
            Console.WriteLine("Testing ADB availability...");
            var isAvailable = await adbService.IsAdbAvailableAsync();
            Console.WriteLine($"ADB Available: {isAvailable}");
            
            if (isAvailable)
            {
                Console.WriteLine("\nGetting devices...");
                var devices = await adbService.GetDevicesAsync();
                
                Console.WriteLine($"Found {devices.Count()} devices:");
                foreach (var device in devices)
                {
                    Console.WriteLine($"  - ID: {device.Id}");
                    Console.WriteLine($"    State: {device.State}");
                    Console.WriteLine($"    Model: {device.Model}");
                    Console.WriteLine($"    Product: {device.Product}");
                    Console.WriteLine($"    Device: {device.Device}");
                    Console.WriteLine($"    IsOnline: {device.IsOnline}");
                    Console.WriteLine();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}