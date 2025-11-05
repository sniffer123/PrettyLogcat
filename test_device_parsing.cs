using System;
using System.Text.RegularExpressions;

class DeviceParsingTest
{
    static void Main()
    {
        // 测试实际的设备输出格式
        string testOutput = @"List of devices attached
emulator-5556          device product:aurora model:24031PN0DC device:aurora transport_id:1";

        Console.WriteLine("Testing device parsing...");
        Console.WriteLine($"Input: {testOutput}");
        Console.WriteLine();

        var lines = testOutput.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines.Skip(1)) // Skip "List of devices attached"
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            Console.WriteLine($"Processing line: '{line}'");

            // 使用正则表达式来解析设备行
            var match = Regex.Match(line.Trim(), @"^(\S+)\s+(\S+)(.*)$");
            if (!match.Success)
            {
                Console.WriteLine("❌ Failed to match regex");
                continue;
            }

            var deviceId = match.Groups[1].Value.Trim();
            var stateString = match.Groups[2].Value.Trim();
            var propertiesString = match.Groups[3].Value.Trim();

            Console.WriteLine($"✅ Device ID: '{deviceId}'");
            Console.WriteLine($"✅ State: '{stateString}'");
            Console.WriteLine($"✅ Properties: '{propertiesString}'");

            // Parse properties
            var modelMatch = Regex.Match(propertiesString, @"model:([^\s]+)");
            var productMatch = Regex.Match(propertiesString, @"product:([^\s]+)");
            var deviceMatch = Regex.Match(propertiesString, @"device:([^\s]+)");

            if (modelMatch.Success)
                Console.WriteLine($"✅ Model: '{modelMatch.Groups[1].Value}'");
            if (productMatch.Success)
                Console.WriteLine($"✅ Product: '{productMatch.Groups[1].Value}'");
            if (deviceMatch.Success)
                Console.WriteLine($"✅ Device: '{deviceMatch.Groups[1].Value}'");
        }

        Console.WriteLine("\nTest completed!");
    }
}