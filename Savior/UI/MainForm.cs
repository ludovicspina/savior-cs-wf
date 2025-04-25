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

        private string _windowsActivationStatus;
        private string _windowsVersion;
        private string _windowsLicenseType;
        private string _windowsProductKeyLast5;


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


        private System.Windows.Forms.Panel sidebar;
        private System.Windows.Forms.Button btnGeneral;
        private System.Windows.Forms.Button btnBSOD;
        private System.Windows.Forms.Button btnVirus;
        private System.Windows.Forms.Button btnInstallation;
        private System.Windows.Forms.Button btnWindows;
        private System.Windows.Forms.Panel panelGeneral;
        private System.Windows.Forms.Label labelCPUName;
        private System.Windows.Forms.Label labelCPUCores;
        private System.Windows.Forms.Label labelRAM;
        private System.Windows.Forms.Label labelDisk;
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
            if (IsInDesignMode())
                return;

            InitializeServices(); 
            // LoadSystemInfo();
            RefreshTemperatures();

            var timer = new System.Windows.Forms.Timer { Interval = 500 };
            timer.Tick += (_, _) => RefreshTemperatures();
            timer.Start();

            _ = Task.Run(async () =>
            {
                await CheckWindowsActivationStatusAsync();
                Invoke(() => { toolStripStatusLabelWindows.Text = _windowsActivationStatus; });
            });
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

        private void LoadSystemInfo()
        {
            if (IsInDesignMode())
                return;

            var cpu = _systemInfo.GetCpuInfo();
            labelCPUName.Text = "Processeur : " + cpu.Name;
            labelCPUCores.Text = $"Cœurs logiques : {cpu.LogicalCores} | Cœurs physiques : {cpu.PhysicalCores}";

            labelRAM.Text = "RAM installée : " + _systemInfo.GetRamInfo() + " Go";
            labelDisk.Text = "Disques :\r\n" + _systemInfo.GetDiskInfo();
            labelGPU.Text = "Carte graphique : " + _systemInfo.GetGpuInfo();
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
                        activationStatus = "✅ Windows est activé.";
                        break;
                    case "0":
                        activationStatus = "❌ Windows n’est pas activé.";
                        break;
                    default:
                        activationStatus = "❓ Statut inconnu.";
                        break;
                }

                // Récupérer le type de licence + les 5 derniers caractères de la clé
                var licenseInfo = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments =
                        "-Command \"Get-CimInstance -Class SoftwareLicensingProduct | Where-Object { $_.PartialProductKey } | Select-Object -First 1 LicenseFamily,PartialProductKey\"",
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
                // Chemin relatif vers le script MAS_AIO.cmd
                string masPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", "MAS_AIO.cmd");

                // Vérifier si le fichier existe
                if (!File.Exists(masPath))
                {
                    MessageBox.Show("Le fichier MAS_AIO.cmd est introuvable. Chemin : " + masPath);
                    return;
                }

                // Configurer le processus pour exécuter le script
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = masPath,
                    UseShellExecute = true,
                    Verb = "runas" // Exécuter en tant qu'administrateur
                };

                // Exécuter le processus
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
            labelCpuTemp = new System.Windows.Forms.Label();
            labelGpuTemp = new System.Windows.Forms.Label();
            AllTabs.SuspendLayout();
            TabSoftwares.SuspendLayout();
            groupBox4.SuspendLayout();
            groupBox3.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // AllTabs
            // 
            AllTabs.AccessibleName = "AllTabs";
            AllTabs.Controls.Add(TabGeneral);
            AllTabs.Controls.Add(TabSoftwares);
            AllTabs.Location = new System.Drawing.Point(12, 12);
            AllTabs.Name = "AllTabs";
            AllTabs.SelectedIndex = 0;
            AllTabs.Size = new System.Drawing.Size(873, 596);
            AllTabs.TabIndex = 0;
            // 
            // TabGeneral
            // 
            TabGeneral.Location = new System.Drawing.Point(4, 24);
            TabGeneral.Name = "TabGeneral";
            TabGeneral.Padding = new System.Windows.Forms.Padding(3);
            TabGeneral.Size = new System.Drawing.Size(865, 568);
            TabGeneral.TabIndex = 0;
            TabGeneral.Text = "General";
            TabGeneral.UseVisualStyleBackColor = true;
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
            TabSoftwares.Size = new System.Drawing.Size(865, 568);
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
            InstallSelection.Size = new System.Drawing.Size(126, 31);
            InstallSelection.TabIndex = 0;
            InstallSelection.Text = "Installer la selection";
            InstallSelection.UseVisualStyleBackColor = true;
            InstallSelection.Click += BtnOpenPowerShell_Click;
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
            // MainForm
            // 
            ClientSize = new System.Drawing.Size(897, 649);
            Controls.Add(labelGpuTemp);
            Controls.Add(labelCpuTemp);
            Controls.Add(AllTabs);
            AllTabs.ResumeLayout(false);
            TabSoftwares.ResumeLayout(false);
            groupBox4.ResumeLayout(false);
            groupBox3.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            ResumeLayout(false);
        }

        private System.Windows.Forms.Label label1;

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