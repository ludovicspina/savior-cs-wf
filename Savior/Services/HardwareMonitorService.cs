using LibreHardwareMonitor.Hardware;
using System.Collections.Generic;

namespace Savior.Services
{
    public class HardwareMonitorService
    {
        private readonly Computer computer;

        public HardwareMonitorService()
        {
            computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true
            };
            computer.Open();
        }

        public float GetCpuAverageTemperature()
        {
            List<float?> temps = new List<float?>();
            foreach (var hardware in computer.Hardware)
            {
                if (hardware.HardwareType == HardwareType.Cpu)
                {
                    hardware.Update();
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                        {
                            temps.Add(sensor.Value);
                        }
                    }
                }
            }

            if (temps.Count == 0)
            {
                return float.NaN; // Retourne NaN si aucune température n'est trouvée
            }

            return temps.Average(t => t.Value);
        }
        
        public float GetCpuRealTemperature()
        {
            float? cpuDieTemp = null;
            float? core1Temp = null;

            foreach (var hardware in computer.Hardware)
            {
                if (hardware.HardwareType == HardwareType.Cpu)
                {
                    hardware.Update();
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                        {
                            if (sensor.Name.Contains("CPU Die", StringComparison.OrdinalIgnoreCase))
                            {
                                cpuDieTemp = sensor.Value;
                            }
                            else if (sensor.Name.Contains("Core #1", StringComparison.OrdinalIgnoreCase))
                            {
                                core1Temp = sensor.Value;
                            }
                        }
                    }
                }
            }

            return cpuDieTemp ?? core1Temp ?? float.NaN;
        }


        public Dictionary<string, float?> GetGpuTemperatures()
        {
            var temps = new Dictionary<string, float?>();
            foreach (var hardware in computer.Hardware)
            {
                if (hardware.HardwareType == HardwareType.GpuNvidia || hardware.HardwareType == HardwareType.GpuAmd)
                {
                    hardware.Update();
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                        {
                            temps[sensor.Name] = sensor.Value;
                        }
                    }
                }
            }
            return temps;
        }
    }
}