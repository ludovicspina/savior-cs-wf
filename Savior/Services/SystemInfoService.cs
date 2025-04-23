using System;
using System.Management;

namespace Savior.Services
{
    public class SystemInfoService
    {
        public (string Name, int LogicalCores, int PhysicalCores) GetCpuInfo()
        {
            var cpuSearcher = new ManagementObjectSearcher("select * from Win32_Processor");
            foreach (var item in cpuSearcher.Get())
            {
                return (
                    item["Name"]?.ToString() ?? "Inconnu",
                    Convert.ToInt32(item["NumberOfLogicalProcessors"]),
                    Convert.ToInt32(item["NumberOfCores"])
                );
            }
            return ("Inconnu", 0, 0);
        }

        public double GetRamInfo()
        {
            var ramSearcher = new ManagementObjectSearcher("select * from Win32_ComputerSystem");
            foreach (var item in ramSearcher.Get())
            {
                return Math.Round(Convert.ToDouble(item["TotalPhysicalMemory"]) / (1024 * 1024 * 1024), 2);
            }
            return 0;
        }

        public string GetDiskInfo()
        {
            string result = "";
            var diskSearcher = new ManagementObjectSearcher("select * from Win32_LogicalDisk where DriveType=3");
            foreach (var item in diskSearcher.Get())
            {
                string name = item["DeviceID"]?.ToString();
                double total = Math.Round(Convert.ToDouble(item["Size"]) / (1024 * 1024 * 1024), 2);
                double free = Math.Round(Convert.ToDouble(item["FreeSpace"]) / (1024 * 1024 * 1024), 2);
                result += $"[{name}] {free} Go libres / {total} Go\r\n";
            }
            return result;
        }

        public string GetGpuInfo()
        {
            var gpuSearcher = new ManagementObjectSearcher("select * from Win32_VideoController");
            foreach (var item in gpuSearcher.Get())
            {
                double mem = Math.Round(Convert.ToDouble(item["AdapterRAM"]) / (1024 * 1024 * 1024), 2);
                return item["Name"] + $" ({mem} Go)";
            }
            return "Inconnu";
        }
    }
}
