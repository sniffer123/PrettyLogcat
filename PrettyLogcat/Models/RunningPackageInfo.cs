namespace PrettyLogcat.Models
{
    // 运行中的包信息类
    public class RunningPackageInfo
    {
        public int Pid { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public string DisplayText => $"{PackageName} (PID: {Pid}) - {ProcessName}";
    }
}