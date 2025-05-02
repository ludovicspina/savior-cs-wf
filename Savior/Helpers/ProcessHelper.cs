using System.Diagnostics;
using System.Threading.Tasks;

namespace Savior.Helpers
{
    public static class ProcessHelper
    {
        public static async Task<string> RunAsync(string fileName, string arguments = "")
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            return output;
        }

        public static async Task RunElevatedAsync(string fileName, string arguments = "")
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = true,
                Verb = "runas" // Exécute en tant qu'admin
            };

            await Task.Run(() => Process.Start(psi));
        }
    }
}