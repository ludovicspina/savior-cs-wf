namespace Savior.Models
{
    public class ProcessInfo
    {
        public int Pid { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public double MemoryMB { get; set; }
        public double CpuPercent { get; set; }
        public bool IsSigned { get; set; }
        public bool IsSuspicious { get; set; }
    }
}