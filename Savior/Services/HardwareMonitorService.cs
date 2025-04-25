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

        public float G()
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
            List<float> temps = new();

            foreach (var hardware in computer.Hardware)
            {
                if (hardware.HardwareType == HardwareType.Cpu)
                {
                    hardware.Update();
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                        {
                            Console.WriteLine($"[CPU SENSOR] {sensor.Name} = {sensor.Value.Value} °C");
                            temps.Add(sensor.Value.Value);
                        }
                    }
                }
            }

            return temps.Count > 0 ? temps.Max() : float.NaN;
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