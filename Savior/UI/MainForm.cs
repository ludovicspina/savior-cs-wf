#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Windows.Forms;
using Savior.Models;
using Savior.Services;
using System.Threading.Tasks;

namespace Savior.UI
{
    public partial class MainForm : Form
    {
        private HardwareMonitorService _hardwareMonitor;
        private SystemInfoService _systemInfo;
        private BsodEventService _bsodService;
        private ProcessScannerService _processScanner;

        private System.Windows.Forms.Timer dotsTimer;
        private int dotsCount = 0;

        private System.Windows.Forms.Button buttonActivation;

        private List<string> bloatwareApps = new List<string>
        {
            "Microsoft.3DBuilder",
            "Microsoft.XboxApp",
            "Microsoft.GetHelp",
            "Microsoft.Getstarted",
            "Microsoft.MicrosoftSolitaireCollection",
            "Microsoft.People",
            "Microsoft.SkypeApp",
            "Microsoft.MicrosoftOfficeHub",
            "Microsoft.MSPaint", // facultatif
            "Microsoft.OneConnect",
            "Microsoft.WindowsMaps",
            "Microsoft.ZuneMusic",
            "Microsoft.ZuneVideo",
            "Microsoft.BingWeather",
            "Microsoft.BingNews",
            "Microsoft.BingFinance",
            "Microsoft.BingSports",
            "Microsoft.MicrosoftStickyNotes",
            "Microsoft.WindowsFeedbackHub"
        };

        private List<string> uselessServices = new List<string>
        {
            "DiagTrack", // Connected User Experience (Télémétrie)
            "dmwappushservice", // Push Notifications (inutile pour 99% des PC)
            "RetailDemo", // Retail Demo Service
            "MapsBroker", // Cartes Windows
            "XblGameSave", // Xbox Live Game Save
            "XboxNetApiSvc", // Xbox Live Networking
            "Fax", // Service de fax
            "WMPNetworkSvc" // Windows Media Player Network Sharing Service
        };


        private string _windowsActivationStatus;
        private string _windowsVersion;
        private string _windowsLicenseType;
        private string _windowsProductKeyLast5;

        private System.Windows.Forms.Label labelCPURef2;
        private System.Windows.Forms.Label labelCPURef;
        private System.Windows.Forms.Label labelGPURef2;
        private System.Windows.Forms.Label labelGPURef;
        private System.Windows.Forms.Label labelRAM;
        private System.Windows.Forms.Label labelCPUCores;
        private System.Windows.Forms.Label labelDisk;


        private System.Windows.Forms.CheckedListBox checkedListBoxServices;
        private System.Windows.Forms.CheckedListBox checkedListBoxApps;
        private System.Windows.Forms.Button buttonOptimisation;
        private System.Windows.Forms.Button buttonBloatWare;

        private System.Windows.Forms.CheckBox checkBoxVLC;
        private System.Windows.Forms.CheckBox checkBox7ZIP;
        private System.Windows.Forms.CheckBox checkBoxChrome;
        private System.Windows.Forms.CheckBox checkBoxAdobe;
        private System.Windows.Forms.CheckBox checkBoxSublimeText;
        private System.Windows.Forms.CheckBox checkBoxLibreOffice;
        private System.Windows.Forms.CheckBox checkBoxKaspersky;
        private System.Windows.Forms.CheckBox checkBoxBitdefender;
        private System.Windows.Forms.CheckBox checkBoxSteam;
        private System.Windows.Forms.CheckBox checkBoxDiscord;

        private System.Windows.Forms.Button buttonWinUpdate;

        private System.Windows.Forms.Label labelWindowsActivation;

        private System.Windows.Forms.Panel sidebar;
        private System.Windows.Forms.Button btnGeneral;
        private System.Windows.Forms.Button btnBSOD;
        private System.Windows.Forms.Button btnVirus;
        private System.Windows.Forms.Button btnInstallation;
        private System.Windows.Forms.Button btnWindows;
        private System.Windows.Forms.Panel panelGeneral;
        private System.Windows.Forms.Label labelCPUName;
        private System.Windows.Forms.Label labelGPU;
        private System.Windows.Forms.Label labelCpuTemp;
        private System.Windows.Forms.Label labelGpuTemp;
        private System.Windows.Forms.Panel panelBSOD;
        private System.Windows.Forms.ListView listViewBSOD;
        private System.Windows.Forms.Panel panelVirus;
        private System.Windows.Forms.ListView listViewVirus;
        private System.Windows.Forms.Button btnKillProcess;
        private System.Windows.Forms.Panel panelInstallation;
        private System.Windows.Forms.Panel panelWindows;
        private System.Windows.Forms.Label labelWindowsStatus;
        private System.Windows.Forms.Button btnActivateWindows;
        private System.Windows.Forms.GroupBox groupBoxSystemInfo;
        private System.Windows.Forms.GroupBox groupBoxTemperatures;
        private System.Windows.Forms.ProgressBar progressBarInstallation;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelCpuTemp;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelGpuTemp;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelWindows;

        public MainForm()
        {
            InitializeComponent();

            if (IsInDesignMode())
                return;

            // Configuration du ListView BSOD
            // listViewBSOD.Columns.Add("Date", 150);
            // listViewBSOD.Columns.Add("Source", 100);
            // listViewBSOD.Columns.Add("ID", 70);
            // listViewBSOD.Columns.Add("Message", 400);

            // Configuration du ListView Virus
            // listViewVirus.Columns.Add("Nom", 150);
            // listViewVirus.Columns.Add("PID", 70);
            // listViewVirus.Columns.Add("Mémoire (MB)", 100);
            // listViewVirus.Columns.Add("Signé", 70);
            // listViewVirus.Columns.Add("Chemin", 400);

            this.Load += MainForm_Load;
        }

        private async void MainForm_Load(object? sender, EventArgs e)
        {
            dotsTimer = new System.Windows.Forms.Timer();
            dotsTimer.Interval = 500; // toutes les 500 ms
            dotsTimer.Tick += (s, e) => AnimateDots();
            dotsTimer.Start();


            if (IsInDesignMode())
                return;

            InitializeServices();
            RefreshTemperatures();
            LoadOptimizationLists();

            var timer = new System.Windows.Forms.Timer { Interval = 500 };
            timer.Tick += (_, _) => RefreshTemperatures();
            timer.Start();

            _ = Task.Run(() =>
            {
                try
                {
                    var cpu = _systemInfo.GetCpuInfo();
                    var gpu = _systemInfo.GetGpuInfo();
                    var ram = _systemInfo.GetRamInfo();
                    var disk = _systemInfo.GetDiskInfo();

                    Console.WriteLine(disk);


                    Invoke(() =>
                    {
                        labelCPURef.Text = cpu.Name;
                        labelCPURef2.Text = cpu.Name;
                        labelCPUCores.Text =
                            $"Cœurs logiques : {cpu.LogicalCores} | Cœurs physiques : {cpu.PhysicalCores}";
                        labelRAM.Text = "RAM installée : " + ram + " Go";
                        labelDisk.Text = "Disques :\r\n" + disk;
                        labelGPURef.Text = gpu;
                        labelGPURef2.Text = gpu;
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Erreur chargement info système : " + ex.Message);
                }
            });

            _ = Task.Run(async () =>
            {
                try
                {
                    Console.WriteLine(">> Lancement de CheckWindowsActivationStatusAsync...");

                    await CheckWindowsActivationStatusAsync(); // 🔥 1) On attend vraiment la fin du check

                    Console.WriteLine(">> Fin de CheckWindowsActivationStatusAsync !");

                    Invoke(() =>
                    {
                        if (dotsTimer != null)
                            dotsTimer.Stop(); // 🔥 2) Seulement ici on stoppe l'animation dots

                        if (toolStripStatusLabelWindows != null)
                            toolStripStatusLabelWindows.Text = _windowsActivationStatus;
                        if (labelWindowsActivation != null)
                            labelWindowsActivation.Text = $"Windows : {_windowsActivationStatus}";
                    });
                }
                catch (Exception ex)
                {
                    Invoke(() =>
                    {
                        if (labelWindowsActivation != null)
                            labelWindowsActivation.Text = $"Erreur : {ex.Message}";
                        else
                            Console.WriteLine($"Erreur (pas de label dispo) : {ex.Message}");
                    });
                }
            });
        }


        private void LoadOptimizationLists()
        {
            // Pour les applications
            checkedListBoxApps.Items.Clear();
            foreach (var app in bloatwareApps)
            {
                bool isChecked = app.Contains("BingSports") ||
                                 app.Contains("BingFinance") || 
                                 app.Contains("BingWeather") || 
                                 app.Contains("ZuneVideo") || 
                                 app.Contains("ZuneMusic") || 
                                 app.Contains("MicrosoftSolitaireCollection") || 
                                 app.Contains("3DBuilder") || 
                                 app.Contains("SkypeApp")
                                 ;
                checkedListBoxApps.Items.Add(app, isChecked);
            }

            // Pour les services
            checkedListBoxServices.Items.Clear();
            foreach (var service in uselessServices)
            {
                bool isChecked = service switch
                {
                    "DiagTrack" => true,
                    "dmwappushservice" => true,
                    "RetailDemo" => true,
                    _ => false
                };
                checkedListBoxServices.Items.Add(service, isChecked);
            }
        }



        private void BtnUninstallSelectedApps_Click(object sender, EventArgs e)
        {
            if (checkedListBoxApps.CheckedItems.Count == 0)
            {
                MessageBox.Show("Aucune application sélectionnée !");
                return;
            }

            string psCommand = "";

            foreach (var item in checkedListBoxApps.CheckedItems)
            {
                string appName = item.ToString();
                psCommand += $@"
$package = Get-AppxPackage -Name '{appName}' -ErrorAction SilentlyContinue
if ($package) {{
    Remove-AppxPackage $package -ErrorAction SilentlyContinue
    Write-Output '{appName} -> Désinstallé'
}} else {{
    Write-Output '{appName} -> Non trouvé'
}}
";
            }

            LaunchPowerShellScript(psCommand);
        }

        private void BtnDisableSelectedServices_Click(object sender, EventArgs e)
        {
            if (checkedListBoxServices.CheckedItems.Count == 0)
            {
                MessageBox.Show("Aucun service sélectionné !");
                return;
            }

            string psCommand = "";

            foreach (var item in checkedListBoxServices.CheckedItems)
            {
                string serviceName = item.ToString();
                psCommand += $@"
$service = Get-Service -Name '{serviceName}' -ErrorAction SilentlyContinue
if ($service) {{
    try {{
        Stop-Service -Name '{serviceName}' -Force -ErrorAction SilentlyContinue
        Set-Service -Name '{serviceName}' -StartupType Disabled
        Write-Output '{serviceName} -> Désactivé'
    }} catch {{
        Write-Output '{serviceName} -> Erreur lors de la désactivation'
    }}
}} else {{
    Write-Output '{serviceName} -> Non trouvé'
}}
";
            }

            LaunchPowerShellScript(psCommand);
        }
        
        private void LaunchPowerShellScript(string scriptContent)
        {
            try
            {
                string tempScriptPath = Path.Combine(Path.GetTempPath(), $"SaviorScript_{Guid.NewGuid()}.ps1");
                File.WriteAllText(tempScriptPath, scriptContent);

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoExit -ExecutionPolicy Bypass -File \"{tempScriptPath}\"",
                    UseShellExecute = true,
                    Verb = "runas" // Admin obligatoire
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'exécution du script PowerShell : {ex.Message}");
            }
        }


        private void ShowPanel(Panel panel)
        {
            panelGeneral.Visible = false;
            panelBSOD.Visible = false;
            panelVirus.Visible = false;
            panelInstallation.Visible = false;
            panelWindows.Visible = false;

            panel.Visible = true;
        }

        private void InitializeServices()
        {
            if (IsInDesignMode())
                return;

            _hardwareMonitor = new HardwareMonitorService();
            _systemInfo = new SystemInfoService();
            _bsodService = new BsodEventService();
            _processScanner = new ProcessScannerService();
        }


        private void RefreshTemperatures()
        {
            Console.WriteLine(">>> Refreshing temps...");

            float cpuRealTemp = _hardwareMonitor.GetCpuRealTemperature();
            Console.WriteLine("CPU TEMP: " + cpuRealTemp);

            var gpuTemps = _hardwareMonitor.GetGpuTemperatures();
            var gpuTempText = gpuTemps.Count > 0
                ? string.Join("  ", gpuTemps.Select(t => $"GPU: {t.Value:F1} °C"))
                : "GPU: 0.0 °C";

            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    labelCpuTemp.Text = $"CPU: {cpuRealTemp:F1} °C";
                    labelGpuTemp.Text = gpuTempText;
                }));
            }
            else
            {
                labelCpuTemp.Text = $"CPU: {cpuRealTemp:F1} °C";
                labelGpuTemp.Text = gpuTempText;
            }

            if (float.IsNaN(cpuRealTemp))
                Console.WriteLine("⚠️ Température CPU non trouvée");
        }


        private void BtnGeneral_Click(object sender, EventArgs e)
        {
            ShowPanel(panelGeneral);
        }

        private void BtnBSOD_Click(object sender, EventArgs e)
        {
            ShowPanel(panelBSOD);

            listViewBSOD.Items.Clear();
            var events = _bsodService.GetRecentBsodEvents();

            foreach (var ev in events)
            {
                SafeAddItem(listViewBSOD, new ListViewItem(new[]
                {
                    ev.Date,
                    ev.Source,
                    ev.EventId,
                    ev.ShortMessage
                }));
            }

            if (events.Count == 0)
            {
                var item = new ListViewItem(new[] { "", "", "", "Aucun événement BSOD trouvé" });
                SafeAddItem(listViewBSOD, item);
            }
        }

        private void BtnVirus_Click(object sender, EventArgs e)
        {
            ShowPanel(panelVirus);

            listViewVirus.Items.Clear();
            var processes = _processScanner.ScanProcesses();

            foreach (var proc in processes)
            {
                var item = new ListViewItem(proc.Name);
                item.SubItems.Add(proc.Pid.ToString());
                item.SubItems.Add(proc.MemoryMB.ToString());
                item.SubItems.Add(proc.IsSigned ? "Oui" : "Non");
                item.SubItems.Add(proc.Path);
                item.Tag = proc;
                SafeAddItem(listViewVirus, item);
            }
        }

        private void BtnInstallation_Click(object sender, EventArgs e)
        {
            ShowPanel(panelInstallation);
        }

        private void BtnWindows_Click(object sender, EventArgs e)
        {
            ShowPanel(panelWindows);
            labelWindowsStatus.Text = $"{_windowsActivationStatus}\n" +
                                      $"Version : {_windowsVersion}\n" +
                                      $"Type de licence : {_windowsLicenseType}\n" +
                                      $"Clé produit : *****-*****-*****-*****-{_windowsProductKeyLast5}";
        }

        private void AnimateDots()
        {
            dotsCount = (dotsCount + 1) % 4; // 0 -> 1 -> 2 -> 3 -> 0
            string dots = new string('.', dotsCount);
            labelWindowsActivation.Text = $"Windows : Chargement{dots}";
        }

        private async Task CheckWindowsActivationStatusAsync()
        {
            try
            {
                string version = Environment.OSVersion.VersionString;
                string activationStatus = "❓ Impossible de déterminer l’état d’activation";
                string licenseType = "❓ Inconnu";
                string productKeyLast5 = "❓";

                // PowerShell pour vérifier l’activation
                var checkActivation = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments =
                        "-Command \"(Get-CimInstance -Class SoftwareLicensingProduct | Where-Object { $_.PartialProductKey } | Select-Object -First 1).LicenseStatus\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                string result = await RunProcessAsync(checkActivation);

                result = result.Trim();
                switch (result)
                {
                    case "1":
                        activationStatus = "✅ Actif";
                        break;
                    case "0":
                        activationStatus = "❌ Inactif";
                        break;
                    default:
                        activationStatus = "❓ Inconnu";
                        break;
                }

                // Récupérer le type de licence + les 5 derniers caractères de la clé
                var licenseInfo = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments =
                        "-Command \"(Get-CimInstance SoftwareLicensingProduct | ? PartialProductKey).LicenseStatus\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                string licenseOutput = await RunProcessAsync(licenseInfo);

                foreach (var line in licenseOutput.Split('\n'))
                {
                    if (line.Contains("OEM"))
                        licenseType = "OEM";
                    else if (line.Contains("Retail"))
                        licenseType = "Retail";
                    else if (line.Contains("Volume"))
                        licenseType = "Volume";

                    if (line.Trim().Length == 5)
                        productKeyLast5 = line.Trim();
                }

                _windowsActivationStatus = activationStatus;
                _windowsVersion = version;
                _windowsLicenseType = licenseType;
                _windowsProductKeyLast5 = productKeyLast5;
            }
            catch (Exception ex)
            {
                _windowsActivationStatus = $"❌ Erreur lors de la vérification : {ex.Message}";
            }
        }

        private async Task<string> RunProcessAsync(ProcessStartInfo startInfo)
        {
            using (var process = new Process { StartInfo = startInfo })
            {
                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
                return output;
            }
        }

        private void BtnActivateWindows_Click(object sender, EventArgs e)
        {
            try
            {
                string masPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", "MAS_AIO.cmd");
                string masDir = Path.GetDirectoryName(masPath)!; // Dossier du script
                Console.WriteLine("Chemin du script MAS : " + masPath);


                if (!File.Exists(masPath))
                {
                    MessageBox.Show("Le fichier MAS_AIO.cmd est introuvable. Chemin : " + masPath);
                    return;
                }

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = masPath, // 🆕 Chemin complet du script
                    WorkingDirectory = masDir, // 🆕 Dossier du script
                    UseShellExecute = true,
                    Verb = "runas" // Exécution en tant qu'admin
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur : " + ex.Message);
            }
        }


        private void BtnKillProcess_Click(object sender, EventArgs e)
        {
            if (listViewVirus.SelectedItems.Count > 0)
            {
                var selected = listViewVirus.SelectedItems[0];
                if (selected.Tag is ProcessInfo proc)
                {
                    _processScanner.KillProcess(proc.Pid);
                    MessageBox.Show($"Processus {proc.Name} (PID: {proc.Pid}) terminé.");
                    BtnVirus_Click(null, null);
                }
            }
        }

        private void ButtonWinUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                string arguments =
                    "-NoExit -Command \"Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force; " +
                    "Install-Module PSWindowsUpdate -Force -Confirm:$false; " +
                    "Import-Module PSWindowsUpdate; " +
                    "Get-WindowsUpdate -Install -AcceptAll -IgnoreReboot\"";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = arguments,
                    UseShellExecute = true,
                    Verb = "runas" // ✅ Ouvre avec élévation (admin)
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur PowerShell: {ex.Message}");
            }
        }


        private void BtnOpenPowerShell_Click(object sender, EventArgs e)
        {
            try
            {
                string arguments = "-NoExit -Command \"";

                if (checkBoxVLC != null && checkBoxVLC.Checked)
                    arguments +=
                        "winget install --id VideoLAN.VLC -e --silent --accept-package-agreements --accept-source-agreements;\n";
                if (checkBox7ZIP != null && checkBox7ZIP.Checked)
                    arguments += "winget install --id 7zip.7zip -e; ";
                if (checkBoxChrome != null && checkBoxChrome.Checked)
                    arguments += "winget install --id Google.Chrome -e; ";
                if (checkBoxAdobe != null && checkBoxAdobe.Checked)
                    arguments += "winget install --id Adobe.Acrobat.Reader.64-bit -e; ";
                if (checkBoxSublimeText != null && checkBoxSublimeText.Checked)
                    arguments += "winget install --id SublimeHQ.SublimeText -e; ";
                if (checkBoxLibreOffice != null && checkBoxLibreOffice.Checked)
                    arguments += "winget install --id TheDocumentFoundation.LibreOffice -e; ";
                if (checkBoxKaspersky != null && checkBoxKaspersky.Checked)
                    arguments += "Start-Process 'https://www.kaspersky.fr/downloads/standard'; ";
                if (checkBoxBitdefender != null && checkBoxBitdefender.Checked)
                    arguments += "winget install --id Bitdefender.Bitdefender -e; ";
                if (checkBoxSteam != null && checkBoxSteam.Checked)
                    arguments += "winget install --id Valve.Steam -e; ";
                if (checkBoxDiscord != null && checkBoxDiscord.Checked)
                    arguments += "winget install --id Discord.Discord -e; ";

                arguments = arguments.TrimEnd(' ', ';') + "\"";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = arguments,
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur PowerShell: {ex.Message}");
            }
        }


        private void SafeAddItem(ListView listView, ListViewItem item)
        {
            if (listView.InvokeRequired)
                listView.Invoke(() => listView.Items.Add(item));
            else
                listView.Items.Add(item);
        }

        private bool IsInDesignMode()
        {
            return LicenseManager.UsageMode == LicenseUsageMode.Designtime || DesignMode;
        }

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            AllTabs = new System.Windows.Forms.TabControl();
            TabGeneral = new System.Windows.Forms.TabPage();
            groupBox5 = new System.Windows.Forms.GroupBox();
            labelDisk = new System.Windows.Forms.Label();
            labelCPUCores = new System.Windows.Forms.Label();
            labelRAM = new System.Windows.Forms.Label();
            labelGPURef2 = new System.Windows.Forms.Label();
            labelCPURef2 = new System.Windows.Forms.Label();
            TabSoftwares = new System.Windows.Forms.TabPage();
            groupBox4 = new System.Windows.Forms.GroupBox();
            checkBoxSteam = new System.Windows.Forms.CheckBox();
            checkBoxDiscord = new System.Windows.Forms.CheckBox();
            groupBox3 = new System.Windows.Forms.GroupBox();
            checkBoxKaspersky = new System.Windows.Forms.CheckBox();
            checkBoxBitdefender = new System.Windows.Forms.CheckBox();
            groupBox2 = new System.Windows.Forms.GroupBox();
            checkBox7ZIP = new System.Windows.Forms.CheckBox();
            checkBoxSublimeText = new System.Windows.Forms.CheckBox();
            groupBox1 = new System.Windows.Forms.GroupBox();
            checkBoxVLC = new System.Windows.Forms.CheckBox();
            checkBoxAdobe = new System.Windows.Forms.CheckBox();
            checkBoxLibreOffice = new System.Windows.Forms.CheckBox();
            checkBoxChrome = new System.Windows.Forms.CheckBox();
            InstallSelection = new System.Windows.Forms.Button();
            tabWinUpdate = new System.Windows.Forms.TabPage();
            buttonWinUpdate = new System.Windows.Forms.Button();
            tabOptimisation = new System.Windows.Forms.TabPage();
            groupBox7 = new System.Windows.Forms.GroupBox();
            checkedListBoxServices = new System.Windows.Forms.CheckedListBox();
            buttonOptimisation = new System.Windows.Forms.Button();
            groupBox6 = new System.Windows.Forms.GroupBox();
            checkedListBoxApps = new System.Windows.Forms.CheckedListBox();
            buttonBloatWare = new System.Windows.Forms.Button();
            labelCpuTemp = new System.Windows.Forms.Label();
            labelGpuTemp = new System.Windows.Forms.Label();
            labelWindowsActivation = new System.Windows.Forms.Label();
            labelCPURef = new System.Windows.Forms.Label();
            labelGPURef = new System.Windows.Forms.Label();
            buttonActivation = new System.Windows.Forms.Button();
            AllTabs.SuspendLayout();
            TabGeneral.SuspendLayout();
            groupBox5.SuspendLayout();
            TabSoftwares.SuspendLayout();
            groupBox4.SuspendLayout();
            groupBox3.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox1.SuspendLayout();
            tabWinUpdate.SuspendLayout();
            tabOptimisation.SuspendLayout();
            groupBox7.SuspendLayout();
            groupBox6.SuspendLayout();
            SuspendLayout();
            // 
            // AllTabs
            // 
            AllTabs.AccessibleName = "AllTabs";
            AllTabs.Controls.Add(TabGeneral);
            AllTabs.Controls.Add(TabSoftwares);
            AllTabs.Controls.Add(tabWinUpdate);
            AllTabs.Controls.Add(tabOptimisation);
            AllTabs.Dock = System.Windows.Forms.DockStyle.Top;
            AllTabs.Location = new System.Drawing.Point(0, 0);
            AllTabs.Name = "AllTabs";
            AllTabs.SelectedIndex = 0;
            AllTabs.Size = new System.Drawing.Size(897, 608);
            AllTabs.TabIndex = 0;
            // 
            // TabGeneral
            // 
            TabGeneral.Controls.Add(groupBox5);
            TabGeneral.Location = new System.Drawing.Point(4, 24);
            TabGeneral.Name = "TabGeneral";
            TabGeneral.Padding = new System.Windows.Forms.Padding(3);
            TabGeneral.Size = new System.Drawing.Size(889, 580);
            TabGeneral.TabIndex = 0;
            TabGeneral.Text = "General";
            TabGeneral.UseVisualStyleBackColor = true;
            // 
            // groupBox5
            // 
            groupBox5.Controls.Add(labelDisk);
            groupBox5.Controls.Add(labelCPUCores);
            groupBox5.Controls.Add(labelRAM);
            groupBox5.Controls.Add(labelGPURef2);
            groupBox5.Controls.Add(labelCPURef2);
            groupBox5.Location = new System.Drawing.Point(19, 17);
            groupBox5.Name = "groupBox5";
            groupBox5.Size = new System.Drawing.Size(483, 251);
            groupBox5.TabIndex = 0;
            groupBox5.TabStop = false;
            groupBox5.Text = "Informations système";
            // 
            // labelDisk
            // 
            labelDisk.Location = new System.Drawing.Point(6, 120);
            labelDisk.Name = "labelDisk";
            labelDisk.Size = new System.Drawing.Size(439, 128);
            labelDisk.TabIndex = 8;
            labelDisk.Text = "DISK NOT FOUND";
            // 
            // labelCPUCores
            // 
            labelCPUCores.Location = new System.Drawing.Point(6, 35);
            labelCPUCores.Name = "labelCPUCores";
            labelCPUCores.Size = new System.Drawing.Size(439, 16);
            labelCPUCores.TabIndex = 7;
            labelCPUCores.Text = "CORES NOT FOUND";
            labelCPUCores.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelRAM
            // 
            labelRAM.Location = new System.Drawing.Point(6, 91);
            labelRAM.Name = "labelRAM";
            labelRAM.Size = new System.Drawing.Size(439, 16);
            labelRAM.TabIndex = 6;
            labelRAM.Text = "RAM NOT FOUND";
            // 
            // labelGPURef2
            // 
            labelGPURef2.Location = new System.Drawing.Point(6, 63);
            labelGPURef2.Name = "labelGPURef2";
            labelGPURef2.Size = new System.Drawing.Size(439, 16);
            labelGPURef2.TabIndex = 5;
            labelGPURef2.Text = "GPU NOT FOUND";
            labelGPURef2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelCPURef2
            // 
            labelCPURef2.Location = new System.Drawing.Point(6, 19);
            labelCPURef2.Name = "labelCPURef2";
            labelCPURef2.Size = new System.Drawing.Size(439, 16);
            labelCPURef2.TabIndex = 4;
            labelCPURef2.Text = "CPU NOT FOUND";
            // 
            // TabSoftwares
            // 
            TabSoftwares.Controls.Add(groupBox4);
            TabSoftwares.Controls.Add(groupBox3);
            TabSoftwares.Controls.Add(groupBox2);
            TabSoftwares.Controls.Add(groupBox1);
            TabSoftwares.Controls.Add(InstallSelection);
            TabSoftwares.Location = new System.Drawing.Point(4, 24);
            TabSoftwares.Name = "TabSoftwares";
            TabSoftwares.Padding = new System.Windows.Forms.Padding(3);
            TabSoftwares.Size = new System.Drawing.Size(889, 580);
            TabSoftwares.TabIndex = 1;
            TabSoftwares.Text = "Softwares";
            TabSoftwares.UseVisualStyleBackColor = true;
            // 
            // groupBox4
            // 
            groupBox4.Controls.Add(checkBoxSteam);
            groupBox4.Controls.Add(checkBoxDiscord);
            groupBox4.Location = new System.Drawing.Point(660, 155);
            groupBox4.Name = "groupBox4";
            groupBox4.Size = new System.Drawing.Size(155, 334);
            groupBox4.TabIndex = 14;
            groupBox4.TabStop = false;
            groupBox4.Text = "Gaming";
            // 
            // checkBoxSteam
            // 
            checkBoxSteam.Location = new System.Drawing.Point(6, 22);
            checkBoxSteam.Name = "checkBoxSteam";
            checkBoxSteam.Size = new System.Drawing.Size(104, 24);
            checkBoxSteam.TabIndex = 9;
            checkBoxSteam.Text = "Steam";
            checkBoxSteam.UseVisualStyleBackColor = true;
            // 
            // checkBoxDiscord
            // 
            checkBoxDiscord.Location = new System.Drawing.Point(6, 52);
            checkBoxDiscord.Name = "checkBoxDiscord";
            checkBoxDiscord.Size = new System.Drawing.Size(104, 24);
            checkBoxDiscord.TabIndex = 10;
            checkBoxDiscord.Text = "Discord";
            checkBoxDiscord.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(checkBoxKaspersky);
            groupBox3.Controls.Add(checkBoxBitdefender);
            groupBox3.Location = new System.Drawing.Point(499, 155);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new System.Drawing.Size(155, 334);
            groupBox3.TabIndex = 13;
            groupBox3.TabStop = false;
            groupBox3.Text = "Antivirus";
            // 
            // checkBoxKaspersky
            // 
            checkBoxKaspersky.Location = new System.Drawing.Point(6, 22);
            checkBoxKaspersky.Name = "checkBoxKaspersky";
            checkBoxKaspersky.Size = new System.Drawing.Size(104, 24);
            checkBoxKaspersky.TabIndex = 7;
            checkBoxKaspersky.Text = "Kaspersky";
            checkBoxKaspersky.UseVisualStyleBackColor = true;
            // 
            // checkBoxBitdefender
            // 
            checkBoxBitdefender.Location = new System.Drawing.Point(6, 49);
            checkBoxBitdefender.Name = "checkBoxBitdefender";
            checkBoxBitdefender.Size = new System.Drawing.Size(104, 24);
            checkBoxBitdefender.TabIndex = 8;
            checkBoxBitdefender.Text = "Bit Defender";
            checkBoxBitdefender.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(checkBox7ZIP);
            groupBox2.Controls.Add(checkBoxSublimeText);
            groupBox2.Location = new System.Drawing.Point(338, 155);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new System.Drawing.Size(155, 334);
            groupBox2.TabIndex = 12;
            groupBox2.TabStop = false;
            groupBox2.Text = "Autres";
            // 
            // checkBox7ZIP
            // 
            checkBox7ZIP.Location = new System.Drawing.Point(6, 22);
            checkBox7ZIP.Name = "checkBox7ZIP";
            checkBox7ZIP.Size = new System.Drawing.Size(104, 24);
            checkBox7ZIP.TabIndex = 5;
            checkBox7ZIP.Text = "7ZIP";
            checkBox7ZIP.UseVisualStyleBackColor = true;
            // 
            // checkBoxSublimeText
            // 
            checkBoxSublimeText.Location = new System.Drawing.Point(6, 52);
            checkBoxSublimeText.Name = "checkBoxSublimeText";
            checkBoxSublimeText.Size = new System.Drawing.Size(104, 24);
            checkBoxSublimeText.TabIndex = 6;
            checkBoxSublimeText.Text = "Sublime Text";
            checkBoxSublimeText.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(checkBoxVLC);
            groupBox1.Controls.Add(checkBoxAdobe);
            groupBox1.Controls.Add(checkBoxLibreOffice);
            groupBox1.Controls.Add(checkBoxChrome);
            groupBox1.Location = new System.Drawing.Point(16, 155);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new System.Drawing.Size(316, 334);
            groupBox1.TabIndex = 11;
            groupBox1.TabStop = false;
            groupBox1.Text = "Setup de base";
            // 
            // checkBoxVLC
            // 
            checkBoxVLC.AccessibleName = "checkBoxVLC";
            checkBoxVLC.Checked = true;
            checkBoxVLC.CheckState = System.Windows.Forms.CheckState.Checked;
            checkBoxVLC.Location = new System.Drawing.Point(6, 22);
            checkBoxVLC.Name = "checkBoxVLC";
            checkBoxVLC.Size = new System.Drawing.Size(104, 24);
            checkBoxVLC.TabIndex = 1;
            checkBoxVLC.Text = "VLC";
            checkBoxVLC.UseVisualStyleBackColor = true;
            // 
            // checkBoxAdobe
            // 
            checkBoxAdobe.AccessibleName = "checkBoxAdobe";
            checkBoxAdobe.Checked = true;
            checkBoxAdobe.CheckState = System.Windows.Forms.CheckState.Checked;
            checkBoxAdobe.Location = new System.Drawing.Point(6, 52);
            checkBoxAdobe.Name = "checkBoxAdobe";
            checkBoxAdobe.Size = new System.Drawing.Size(104, 24);
            checkBoxAdobe.TabIndex = 2;
            checkBoxAdobe.Text = "Adobe";
            checkBoxAdobe.UseVisualStyleBackColor = true;
            // 
            // checkBoxLibreOffice
            // 
            checkBoxLibreOffice.Checked = true;
            checkBoxLibreOffice.CheckState = System.Windows.Forms.CheckState.Checked;
            checkBoxLibreOffice.Location = new System.Drawing.Point(147, 22);
            checkBoxLibreOffice.Name = "checkBoxLibreOffice";
            checkBoxLibreOffice.Size = new System.Drawing.Size(104, 24);
            checkBoxLibreOffice.TabIndex = 3;
            checkBoxLibreOffice.Text = "Libre Office";
            checkBoxLibreOffice.UseVisualStyleBackColor = true;
            // 
            // checkBoxChrome
            // 
            checkBoxChrome.Checked = true;
            checkBoxChrome.CheckState = System.Windows.Forms.CheckState.Checked;
            checkBoxChrome.Location = new System.Drawing.Point(147, 52);
            checkBoxChrome.Name = "checkBoxChrome";
            checkBoxChrome.Size = new System.Drawing.Size(104, 24);
            checkBoxChrome.TabIndex = 4;
            checkBoxChrome.Text = "Chrome";
            checkBoxChrome.UseVisualStyleBackColor = true;
            // 
            // InstallSelection
            // 
            InstallSelection.Location = new System.Drawing.Point(22, 19);
            InstallSelection.Name = "InstallSelection";
            InstallSelection.Size = new System.Drawing.Size(202, 31);
            InstallSelection.TabIndex = 0;
            InstallSelection.Text = "Installer la selection";
            InstallSelection.UseVisualStyleBackColor = true;
            InstallSelection.Click += BtnOpenPowerShell_Click;
            // 
            // tabWinUpdate
            // 
            tabWinUpdate.Controls.Add(buttonWinUpdate);
            tabWinUpdate.Location = new System.Drawing.Point(4, 24);
            tabWinUpdate.Name = "tabWinUpdate";
            tabWinUpdate.Padding = new System.Windows.Forms.Padding(3);
            tabWinUpdate.Size = new System.Drawing.Size(889, 580);
            tabWinUpdate.TabIndex = 2;
            tabWinUpdate.Text = "Windows";
            tabWinUpdate.UseVisualStyleBackColor = true;
            // 
            // buttonWinUpdate
            // 
            buttonWinUpdate.Location = new System.Drawing.Point(12, 21);
            buttonWinUpdate.Name = "buttonWinUpdate";
            buttonWinUpdate.Size = new System.Drawing.Size(144, 31);
            buttonWinUpdate.TabIndex = 0;
            buttonWinUpdate.Text = "Windows Update";
            buttonWinUpdate.UseVisualStyleBackColor = true;
            buttonWinUpdate.Click += ButtonWinUpdate_Click;
            // 
            // tabOptimisation
            // 
            tabOptimisation.Controls.Add(groupBox7);
            tabOptimisation.Controls.Add(groupBox6);
            tabOptimisation.Location = new System.Drawing.Point(4, 24);
            tabOptimisation.Name = "tabOptimisation";
            tabOptimisation.Padding = new System.Windows.Forms.Padding(3);
            tabOptimisation.Size = new System.Drawing.Size(889, 580);
            tabOptimisation.TabIndex = 3;
            tabOptimisation.Text = "Optimisation";
            tabOptimisation.UseVisualStyleBackColor = true;
            // 
            // groupBox7
            // 
            groupBox7.Controls.Add(checkedListBoxServices);
            groupBox7.Controls.Add(buttonOptimisation);
            groupBox7.Location = new System.Drawing.Point(485, 15);
            groupBox7.Name = "groupBox7";
            groupBox7.Size = new System.Drawing.Size(396, 549);
            groupBox7.TabIndex = 3;
            groupBox7.TabStop = false;
            groupBox7.Text = "Services";
            // 
            // checkedListBoxServices
            // 
            checkedListBoxServices.FormattingEnabled = true;
            checkedListBoxServices.Location = new System.Drawing.Point(6, 56);
            checkedListBoxServices.Name = "checkedListBoxServices";
            checkedListBoxServices.Size = new System.Drawing.Size(384, 472);
            checkedListBoxServices.TabIndex = 2;
            // 
            // buttonOptimisation
            // 
            buttonOptimisation.Location = new System.Drawing.Point(121, 22);
            buttonOptimisation.Name = "buttonOptimisation";
            buttonOptimisation.Size = new System.Drawing.Size(166, 28);
            buttonOptimisation.TabIndex = 1;
            buttonOptimisation.Text = "Optimisation des services";
            buttonOptimisation.UseVisualStyleBackColor = true;
            buttonOptimisation.Click += BtnDisableSelectedServices_Click;
            // 
            // groupBox6
            // 
            groupBox6.Controls.Add(checkedListBoxApps);
            groupBox6.Controls.Add(buttonBloatWare);
            groupBox6.Location = new System.Drawing.Point(12, 15);
            groupBox6.Name = "groupBox6";
            groupBox6.Size = new System.Drawing.Size(396, 549);
            groupBox6.TabIndex = 2;
            groupBox6.TabStop = false;
            groupBox6.Text = "Bloatwares";
            // 
            // checkedListBoxApps
            // 
            checkedListBoxApps.FormattingEnabled = true;
            checkedListBoxApps.Location = new System.Drawing.Point(6, 56);
            checkedListBoxApps.Name = "checkedListBoxApps";
            checkedListBoxApps.Size = new System.Drawing.Size(384, 472);
            checkedListBoxApps.TabIndex = 1;
            // 
            // buttonBloatWare
            // 
            buttonBloatWare.Location = new System.Drawing.Point(114, 22);
            buttonBloatWare.Name = "buttonBloatWare";
            buttonBloatWare.Size = new System.Drawing.Size(166, 28);
            buttonBloatWare.TabIndex = 0;
            buttonBloatWare.Text = "Remove Bloatwares";
            buttonBloatWare.UseVisualStyleBackColor = true;
            buttonBloatWare.Click += BtnUninstallSelectedApps_Click;
            // 
            // labelCpuTemp
            // 
            labelCpuTemp.AccessibleName = "labelCpuTemp";
            labelCpuTemp.Location = new System.Drawing.Point(16, 611);
            labelCpuTemp.Name = "labelCpuTemp";
            labelCpuTemp.Size = new System.Drawing.Size(100, 16);
            labelCpuTemp.TabIndex = 1;
            labelCpuTemp.Text = "CPU : -- °C";
            // 
            // labelGpuTemp
            // 
            labelGpuTemp.AccessibleName = "labelGpuTemp";
            labelGpuTemp.Location = new System.Drawing.Point(16, 627);
            labelGpuTemp.Name = "labelGpuTemp";
            labelGpuTemp.Size = new System.Drawing.Size(100, 16);
            labelGpuTemp.TabIndex = 2;
            labelGpuTemp.Text = "GPU : -- °C";
            // 
            // labelWindowsActivation
            // 
            labelWindowsActivation.Location = new System.Drawing.Point(567, 620);
            labelWindowsActivation.Name = "labelWindowsActivation";
            labelWindowsActivation.Size = new System.Drawing.Size(176, 23);
            labelWindowsActivation.TabIndex = 0;
            labelWindowsActivation.Text = "Windows :";
            // 
            // labelCPURef
            // 
            labelCPURef.Location = new System.Drawing.Point(122, 611);
            labelCPURef.Name = "labelCPURef";
            labelCPURef.Size = new System.Drawing.Size(439, 16);
            labelCPURef.TabIndex = 3;
            labelCPURef.Text = "CPU NOT FOUND";
            // 
            // labelGPURef
            // 
            labelGPURef.Location = new System.Drawing.Point(122, 627);
            labelGPURef.Name = "labelGPURef";
            labelGPURef.Size = new System.Drawing.Size(439, 16);
            labelGPURef.TabIndex = 4;
            labelGPURef.Text = "GPU NOT FOUND";
            // 
            // buttonActivation
            // 
            buttonActivation.Location = new System.Drawing.Point(775, 616);
            buttonActivation.Name = "buttonActivation";
            buttonActivation.Size = new System.Drawing.Size(75, 23);
            buttonActivation.TabIndex = 5;
            buttonActivation.Text = "Activer";
            buttonActivation.UseVisualStyleBackColor = true;
            buttonActivation.Click += BtnActivateWindows_Click;
            // 
            // MainForm
            // 
            ClientSize = new System.Drawing.Size(897, 649);
            Controls.Add(buttonActivation);
            Controls.Add(labelGPURef);
            Controls.Add(labelCPURef);
            Controls.Add(labelWindowsActivation);
            Controls.Add(labelGpuTemp);
            Controls.Add(labelCpuTemp);
            Controls.Add(AllTabs);
            AllTabs.ResumeLayout(false);
            TabGeneral.ResumeLayout(false);
            groupBox5.ResumeLayout(false);
            TabSoftwares.ResumeLayout(false);
            groupBox4.ResumeLayout(false);
            groupBox3.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            tabWinUpdate.ResumeLayout(false);
            tabOptimisation.ResumeLayout(false);
            groupBox7.ResumeLayout(false);
            groupBox6.ResumeLayout(false);
            ResumeLayout(false);
        }


        private System.Windows.Forms.GroupBox groupBox6;
        private System.Windows.Forms.GroupBox groupBox7;

        private System.Windows.Forms.TabPage tabOptimisation;

        private System.Windows.Forms.TabPage tabWinUpdate;

        private System.Windows.Forms.GroupBox groupBox5;

        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.GroupBox groupBox4;

        private System.Windows.Forms.GroupBox groupBox2;

        private System.Windows.Forms.GroupBox groupBox1;

        private System.Windows.Forms.Button InstallSelection;

        private System.Windows.Forms.TabControl AllTabs;
        private System.Windows.Forms.TabPage TabGeneral;
        private System.Windows.Forms.TabPage TabSoftwares;
    }
}