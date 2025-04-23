using Savior.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Security.Cryptography.X509Certificates;
using Process = Savior.Models.ProcessInfo;

namespace Savior.Services
{
    public class ProcessScannerService
    {
        public List<Process> ScanProcesses()
        {
            var result = new List<Process>();
            var counters = new Dictionary<int, PerformanceCounter>();

            foreach (var process in System.Diagnostics.Process.GetProcesses())
            {
                try
                {
                    string path = process.MainModule?.FileName ?? "Inconnu";
                    string name = process.ProcessName;
                    double memoryMb = process.WorkingSet64 / (1024.0 * 1024.0);

                    bool isSigned = CheckSignature(path);
                    bool isSuspicious = false;

                    // Critère : exécutable système hors chemin système
                    if ((name.ToLower() == "svchost" || name.ToLower() == "explorer") &&
                        !path.ToLower().Contains("system32"))
                        isSuspicious = true;

                    // Critère : non signé
                    if (!isSigned)
                        isSuspicious = true;

                    // Critère : mémoire > 500MB
                    if (memoryMb > 500)
                        isSuspicious = true;

                    result.Add(new ProcessInfo
                    {
                        Pid = process.Id,
                        Name = name,
                        Path = path,
                        MemoryMB = Math.Round(memoryMb, 1),
                        CpuPercent = 0, // Optionnel à calculer plus tard
                        IsSigned = isSigned,
                        IsSuspicious = isSuspicious
                    });
                }
                catch
                {
                    // Certains process système ne sont pas accessibles, on ignore
                }
            }

            return result;
        }

        private bool CheckSignature(string filePath)
        {
            try
            {
                var cert = X509Certificate.CreateFromSignedFile(filePath);
                return cert != null;
            }
            catch
            {
                return false;
            }
        }

        public void KillProcess(int pid)
        {
            try
            {
                System.Diagnostics.Process.GetProcessById(pid).Kill();
            }
            catch
            {
                // Gérer les erreurs ou permissions insuffisantes
            }
        }
    }
}