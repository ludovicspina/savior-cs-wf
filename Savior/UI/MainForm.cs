using System.ComponentModel;
using System.Diagnostics;
using Savior.Services;
using Savior.Constants;
using System.Text;


namespace Savior.UI
{
    public partial class MainForm : Form
    {
        private HardwareMonitorService _hardwareMonitor;
        private SystemInfoService _systemInfo;

        private Button _buttonActivation;

        private TabPage _tabStress;
        private Button _buttonStressCpu;
        private Button _buttonStressGPU;
        private Button buttonStressBOTH;

        private Process? furmarkProcess = null;
        private CancellationTokenSource? stressCancellationTokenSource = null;
        private bool isCpuStressRunning = false;
        private bool isGpuStressRunning = false;


        private string _windowsActivationStatus;

        private Label labelCPURef2;
        private Label labelCPURef;
        private Label labelGPURef2;
        private Label labelGPURef;
        private Label labelRAM;
        private Label labelCPUCores;
        private Label labelDisk;


        private CheckedListBox checkedListBoxServices;
        private CheckedListBox checkedListBoxApps;
        private Button buttonOptimisation;
        private Button buttonBloatWare;

        private CheckBox checkBoxVLC;
        private CheckBox checkBox7ZIP;
        private CheckBox checkBoxChrome;
        private CheckBox checkBoxAdobe;
        private CheckBox checkBoxSublimeText;
        private CheckBox checkBoxLibreOffice;
        private CheckBox checkBoxKaspersky;
        private CheckBox checkBoxBitdefender;
        private CheckBox checkBoxSteam;
        private CheckBox checkBoxDiscord;
        private CheckBox checkBoxTeams;
        private CheckBox checkBoxTreeSize;
        private CheckBox checkBoxHDDS;

        private Button buttonWinUpdate;

        private Label labelWindowsActivation;

        private Label labelGPU;
        private Label labelCpuTemp;
        private Label labelGpuTemp;

        private ToolStripStatusLabel toolStripStatusLabelWindows;

        public MainForm()
        {
            InitializeComponent();
            if (IsInDesignMode())
                return;
            this.Icon = new Icon("Data/blacklotus.ico");
            this.Load += MainForm_Load;
        }

        private async void MainForm_Load(object? sender, EventArgs e)
        {
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
                    await CheckWindowsActivationStatusAsync();

                    Invoke(() =>
                    {
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

        private void InitializeServices()
        {
            if (IsInDesignMode())
                return;

            _hardwareMonitor = new HardwareMonitorService();
            _systemInfo = new SystemInfoService();
        }
        
        private async Task CreateShortcutsAsync()
        {
            string scriptPath =
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", "AddDesktopShortcuts.ps1");

            if (!File.Exists(scriptPath))
                throw new FileNotFoundException("Script AddDesktopShortcuts.ps1 introuvable.");

            RunPowerShellScript(scriptPath);
            await Task.Delay(1000);
        }

        private async Task ActivateWindowsIfNeededAsync()
        {
            if (!_windowsActivationStatus.Contains("Activé"))
            {
                string masPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", "MAS_AIO.cmd");
                if (!File.Exists(masPath))
                {
                    MessageBox.Show("MAS_AIO.cmd non trouvé.");
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = masPath,
                    WorkingDirectory = Path.GetDirectoryName(masPath),
                    UseShellExecute = true,
                    Verb = "runas"
                });

                await Task.Delay(1000);
            }
        }

        private async Task RunWindowsUpdateAsync()
        {
            string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", "WindowsUpdate.ps1");

            if (!File.Exists(scriptPath))
                throw new FileNotFoundException("Script WindowsUpdate.ps1 introuvable.");

            RunPowerShellScript(scriptPath);
            await Task.Delay(1000);
        }

        private async Task InstallEssentialAppsAsync()
        {
            string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", "InstallApps.ps1");

            if (!File.Exists(scriptPath))
                throw new FileNotFoundException("Script InstallApps.ps1 introuvable.");

            RunPowerShellScript(scriptPath);
            await Task.Delay(1000);
        }

        private async Task RemoveBloatwaresAsync()
        {
            var script = new StringBuilder();

            foreach (var item in checkedListBoxApps.Items)
            {
                if (checkedListBoxApps.GetItemChecked(checkedListBoxApps.Items.IndexOf(item)))
                {
                    string appName = item.ToString();
                    script.AppendLine($@"
$package = Get-AppxPackage -Name '{appName}' -ErrorAction SilentlyContinue
if ($package) {{
    Remove-AppxPackage $package -ErrorAction SilentlyContinue
    Write-Output '{appName} -> Désinstallé'
}} else {{
    Write-Output '{appName} -> Non trouvé'
}}");
                }
            }

            if (script.Length > 0)
            {
                script.AppendLine("Start-Sleep -Seconds 5");
                script.AppendLine("exit");
                RunPowerShellScript(script.ToString());
                await Task.Delay(1000);
            }
        }

        private async Task DisableUnwantedServicesAsync()
        {
            var selectedServices = new List<string>();

            foreach (var item in checkedListBoxServices.Items)
            {
                if (checkedListBoxServices.GetItemChecked(checkedListBoxServices.Items.IndexOf(item)))
                {
                    selectedServices.Add(item.ToString());
                }
            }

            if (selectedServices.Any())
            {
                string scriptPath =
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", "DisableServices.ps1");

                if (!File.Exists(scriptPath))
                    throw new FileNotFoundException("Script DisableServices.ps1 introuvable.");

                string joinedServices = string.Join(",", selectedServices.Select(s => s.Replace("'", "''")));

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\" -Services \"{joinedServices}\"",
                    UseShellExecute = true,
                    Verb = "runas"
                };

                Process.Start(psi);
                await Task.Delay(1000);
            }
        }

        private void RunPowerShellScript(string scriptPathOrContent)
        {
            string tempFile = scriptPathOrContent;

            if (!File.Exists(scriptPathOrContent))
            {
                // Si on passe un contenu directement, l’écrire dans un fichier temporaire
                tempFile = Path.Combine(Path.GetTempPath(), $"tmp_{Guid.NewGuid()}.ps1");
                File.WriteAllText(tempFile, scriptPathOrContent);
            }

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -File \"{tempFile}\"",
                UseShellExecute = true,
                Verb = "runas"
            };

            Process.Start(psi);
        }

        private void LoadOptimizationLists()
        {
            // Pour les applications
            checkedListBoxApps.Items.Clear();
            foreach (var app in BloatwareLists.Apps)
            {
                bool isChecked = app.Contains("BingSports") ||
                                 app.Contains("BingFinance") ||
                                 app.Contains("BingWeather") ||
                                 app.Contains("ZuneVideo") ||
                                 app.Contains("ZuneMusic") ||
                                 app.Contains("MicrosoftSolitaireCollection") ||
                                 app.Contains("3DBuilder") ||
                                 app.Contains("OneConnect") ||
                                 app.Contains("SkypeApp");
                checkedListBoxApps.Items.Add(app, isChecked);
            }

            // Pour les services
            checkedListBoxServices.Items.Clear();
            foreach (var service in BloatwareLists.Services)
            {
                bool isChecked = service switch
                {
                    "DiagTrack" => true,
                    "dmwappushservice" => true,
                    "RetailDemo" => true,
                    "Fax" => true,
                    _ => false
                };
                checkedListBoxServices.Items.Add(service, isChecked);
            }
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
        
        private void RefreshTemperatures()
        {
            // Console.WriteLine(">>> Refreshing temps...");

            float cpuRealTemp = _hardwareMonitor.GetCpuRealTemperature();
            // Console.WriteLine("CPU TEMP: " + cpuRealTemp);

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
        
        private async void StartCpuStress()
        {
            if (isCpuStressRunning)
                return;

            isCpuStressRunning = true;
            stressCancellationTokenSource = new CancellationTokenSource();

            await Task.Run(() =>
            {
                try
                {
                    while (!stressCancellationTokenSource.Token.IsCancellationRequested)
                    {
                        // Charge CPU simple
                        double x = Math.Pow(12345.6789, 9876.5432);

                        // Vérifie température CPU
                        float cpuTemp = _hardwareMonitor.GetCpuRealTemperature();
                        if (cpuTemp > 85)
                        {
                            Invoke(() =>
                                MessageBox.Show($"⚠️ Température CPU trop haute ({cpuTemp} °C) ! Test arrêté."));
                            StopCpuStress();
                            break;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Normal : annulé
                }
            }, stressCancellationTokenSource.Token);
        }

        private void StopCpuStress()
        {
            isCpuStressRunning = false;
            stressCancellationTokenSource?.Cancel();
        }
        
        private void StartGpuStress()
        {
            try
            {
                string furmarkPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    @"Data\FurMark_win64\FurMark_GUI.exe");

                if (!File.Exists(furmarkPath))
                {
                    MessageBox.Show("FurMark non trouvé : " + furmarkPath);
                    return;
                }

                var psi = new ProcessStartInfo
                {
                    FileName = furmarkPath,
                    UseShellExecute = true,
                    Verb = "runas"
                };

                furmarkProcess = Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lancement FurMark : {ex.Message}");
            }
        }

        private async void BtnMultimediaSetup_Click(object sender, EventArgs e)
        {
            try
            {
                await CreateShortcutsAsync();
                await ActivateWindowsIfNeededAsync();
                await RunWindowsUpdateAsync();
                await InstallEssentialAppsAsync();
                await RemoveBloatwaresAsync();
                await DisableUnwantedServicesAsync();

                MessageBox.Show("Setup multimédia terminé ✅");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur dans le setup multimédia : {ex.Message}");
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
        
        private void BtnStressCpu_Click(object sender, EventArgs e)
        {
            if (!isCpuStressRunning)
                StartCpuStress();
            else
                StopCpuStress();
        }

        private void BtnStressGpu_Click(object sender, EventArgs e)
        {
            StartGpuStress();
        }

        private void BtnStressBoth_Click(object sender, EventArgs e)
        {
            StartCpuStress();
            StartGpuStress();
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

        private void ButtonWinUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                string arguments =
                    "-NoExit -ExecutionPolicy Bypass -Command \"Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force; " +
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
                        "winget install --id VideoLAN.VLC -e --silent --accept-package-agreements --accept-source-agreements; ";
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
                if (checkBoxTeams != null && checkBoxTeams.Checked)
                    arguments += "winget install --id Microsoft.Teams -e; ";
                if (checkBoxTreeSize != null && checkBoxTreeSize.Checked)
                    arguments += "winget install --id JAMSoftware.TreeSize -e; ";
                if (checkBoxHDDS != null && checkBoxHDDS.Checked)
                    arguments += "winget install --id JanosMathe.HardDiskSentinel -e; ";


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
        
        private async Task CheckWindowsActivationStatusAsync()
        {
            try
            {
                string activationStatus = "❓ Inconnu";
                string script = @"
Get-WmiObject -Query 'SELECT Name, LicenseStatus FROM SoftwareLicensingProduct WHERE PartialProductKey IS NOT NULL' |
    Select-Object -Property Name, LicenseStatus";

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"" + script.Replace("\"", "`\"") + "\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                string output = await RunProcessAsync(psi);

                // Analyse de la sortie
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var statusLines = lines
                    .Where(l => l.Contains("Windows"))
                    .ToList();


                // Recherche du premier produit avec statut actif
                foreach (var line in statusLines)
                {
                    Console.WriteLine(line);
                    if (line.Contains("1"))
                    {
                        activationStatus = "✅ Actif";
                        break;
                    }

                    if (line.Contains("0"))
                    {
                        activationStatus = "❌ Inactif";
                    }
                }

                _windowsActivationStatus = activationStatus;
            }
            catch (Exception ex)
            {
                _windowsActivationStatus = $"❌ Erreur : {ex.Message}";
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
            System.ComponentModel.ComponentResourceManager resources =
                new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            AllTabs = new TabControl();
            TabGeneral = new TabPage();
            groupBox10 = new GroupBox();
            label1 = new Label();
            buttonBasicInstallGeneral = new Button();
            groupBox5 = new GroupBox();
            labelDisk = new Label();
            labelCPUCores = new Label();
            labelRAM = new Label();
            labelGPURef2 = new Label();
            labelCPURef2 = new Label();
            TabSoftwares = new TabPage();
            groupBox4 = new GroupBox();
            checkBoxSteam = new CheckBox();
            checkBoxDiscord = new CheckBox();
            groupBox3 = new GroupBox();
            checkBoxKaspersky = new CheckBox();
            checkBoxBitdefender = new CheckBox();
            groupBox2 = new GroupBox();
            checkBoxHDDS = new CheckBox();
            checkBoxTreeSize = new CheckBox();
            groupBox1 = new GroupBox();
            checkBoxSublimeText = new CheckBox();
            checkBox7ZIP = new CheckBox();
            checkBoxTeams = new CheckBox();
            checkBoxVLC = new CheckBox();
            checkBoxAdobe = new CheckBox();
            checkBoxLibreOffice = new CheckBox();
            checkBoxChrome = new CheckBox();
            InstallSelection = new Button();
            tabWinUpdate = new TabPage();
            buttonWinUpdate = new Button();
            tabOptimisation = new TabPage();
            groupBox7 = new GroupBox();
            checkedListBoxServices = new CheckedListBox();
            buttonOptimisation = new Button();
            groupBox6 = new GroupBox();
            checkedListBoxApps = new CheckedListBox();
            buttonBloatWare = new Button();
            _tabStress = new TabPage();
            groupBox9 = new GroupBox();
            _buttonStressGPU = new Button();
            groupBox8 = new GroupBox();
            _buttonStressCpu = new Button();
            buttonStressBOTH = new Button();
            labelCpuTemp = new Label();
            labelGpuTemp = new Label();
            labelWindowsActivation = new Label();
            labelCPURef = new Label();
            labelGPURef = new Label();
            _buttonActivation = new Button();
            AllTabs.SuspendLayout();
            TabGeneral.SuspendLayout();
            groupBox10.SuspendLayout();
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
            _tabStress.SuspendLayout();
            groupBox9.SuspendLayout();
            groupBox8.SuspendLayout();
            SuspendLayout();
            // 
            // AllTabs
            // 
            AllTabs.AccessibleName = "AllTabs";
            AllTabs.Controls.Add(TabGeneral);
            AllTabs.Controls.Add(TabSoftwares);
            AllTabs.Controls.Add(tabWinUpdate);
            AllTabs.Controls.Add(tabOptimisation);
            AllTabs.Controls.Add(_tabStress);
            AllTabs.Dock = DockStyle.Top;
            AllTabs.Location = new Point(0, 0);
            AllTabs.Name = "AllTabs";
            AllTabs.SelectedIndex = 0;
            AllTabs.Size = new Size(897, 608);
            AllTabs.TabIndex = 0;
            // 
            // TabGeneral
            // 
            TabGeneral.Controls.Add(groupBox10);
            TabGeneral.Controls.Add(groupBox5);
            TabGeneral.Location = new Point(4, 24);
            TabGeneral.Name = "TabGeneral";
            TabGeneral.Padding = new Padding(3);
            TabGeneral.Size = new Size(889, 580);
            TabGeneral.TabIndex = 0;
            TabGeneral.Text = "General";
            TabGeneral.UseVisualStyleBackColor = true;
            // 
            // groupBox10
            // 
            groupBox10.Controls.Add(label1);
            groupBox10.Controls.Add(buttonBasicInstallGeneral);
            groupBox10.Location = new Point(19, 274);
            groupBox10.Name = "groupBox10";
            groupBox10.Size = new Size(200, 161);
            groupBox10.TabIndex = 1;
            groupBox10.TabStop = false;
            groupBox10.Text = "Installation basique";
            // 
            // label1
            // 
            label1.Location = new Point(6, 19);
            label1.Name = "label1";
            label1.Size = new Size(174, 103);
            label1.TabIndex = 3;
            label1.Text = ("Installation de VLC, Adobe, Chrome et LibreOffice. Lancement du script MAS d\'acti" +
                           "vation Windows.    Lancement du powershell pour les MAJ Windows.");
            // 
            // buttonBasicInstallGeneral
            // 
            buttonBasicInstallGeneral.Location = new Point(54, 125);
            buttonBasicInstallGeneral.Name = "buttonBasicInstallGeneral";
            buttonBasicInstallGeneral.Size = new Size(75, 23);
            buttonBasicInstallGeneral.TabIndex = 2;
            buttonBasicInstallGeneral.Text = "Installer";
            buttonBasicInstallGeneral.UseVisualStyleBackColor = true;
            buttonBasicInstallGeneral.Click += BtnMultimediaSetup_Click;
            // 
            // groupBox5
            // 
            groupBox5.BackColor = Color.Transparent;
            groupBox5.Controls.Add(labelDisk);
            groupBox5.Controls.Add(labelCPUCores);
            groupBox5.Controls.Add(labelRAM);
            groupBox5.Controls.Add(labelGPURef2);
            groupBox5.Controls.Add(labelCPURef2);
            groupBox5.Location = new Point(19, 17);
            groupBox5.Name = "groupBox5";
            groupBox5.Size = new Size(483, 251);
            groupBox5.TabIndex = 0;
            groupBox5.TabStop = false;
            groupBox5.Text = "Informations système";
            // 
            // labelDisk
            // 
            labelDisk.Location = new Point(6, 120);
            labelDisk.Name = "labelDisk";
            labelDisk.Size = new Size(439, 128);
            labelDisk.TabIndex = 8;
            labelDisk.Text = "DISK NOT FOUND";
            // 
            // labelCPUCores
            // 
            labelCPUCores.Location = new Point(6, 35);
            labelCPUCores.Name = "labelCPUCores";
            labelCPUCores.Size = new Size(439, 16);
            labelCPUCores.TabIndex = 7;
            labelCPUCores.Text = "CORES NOT FOUND";
            labelCPUCores.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // labelRAM
            // 
            labelRAM.Location = new Point(6, 91);
            labelRAM.Name = "labelRAM";
            labelRAM.Size = new Size(439, 16);
            labelRAM.TabIndex = 6;
            labelRAM.Text = "RAM NOT FOUND";
            // 
            // labelGPURef2
            // 
            labelGPURef2.Location = new Point(6, 63);
            labelGPURef2.Name = "labelGPURef2";
            labelGPURef2.Size = new Size(439, 16);
            labelGPURef2.TabIndex = 5;
            labelGPURef2.Text = "GPU NOT FOUND";
            labelGPURef2.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // labelCPURef2
            // 
            labelCPURef2.Location = new Point(6, 19);
            labelCPURef2.Name = "labelCPURef2";
            labelCPURef2.Size = new Size(439, 16);
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
            TabSoftwares.Location = new Point(4, 24);
            TabSoftwares.Name = "TabSoftwares";
            TabSoftwares.Padding = new Padding(3);
            TabSoftwares.Size = new Size(889, 580);
            TabSoftwares.TabIndex = 1;
            TabSoftwares.Text = "Softwares";
            TabSoftwares.UseVisualStyleBackColor = true;
            // 
            // groupBox4
            // 
            groupBox4.Controls.Add(checkBoxSteam);
            groupBox4.Controls.Add(checkBoxDiscord);
            groupBox4.Location = new Point(499, 155);
            groupBox4.Name = "groupBox4";
            groupBox4.Size = new Size(155, 334);
            groupBox4.TabIndex = 14;
            groupBox4.TabStop = false;
            groupBox4.Text = "Gaming";
            // 
            // checkBoxSteam
            // 
            checkBoxSteam.Location = new Point(6, 22);
            checkBoxSteam.Name = "checkBoxSteam";
            checkBoxSteam.Size = new Size(104, 24);
            checkBoxSteam.TabIndex = 9;
            checkBoxSteam.Text = "Steam";
            checkBoxSteam.UseVisualStyleBackColor = true;
            // 
            // checkBoxDiscord
            // 
            checkBoxDiscord.Location = new Point(6, 52);
            checkBoxDiscord.Name = "checkBoxDiscord";
            checkBoxDiscord.Size = new Size(104, 24);
            checkBoxDiscord.TabIndex = 10;
            checkBoxDiscord.Text = "Discord";
            checkBoxDiscord.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(checkBoxKaspersky);
            groupBox3.Controls.Add(checkBoxBitdefender);
            groupBox3.Location = new Point(338, 155);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(155, 334);
            groupBox3.TabIndex = 13;
            groupBox3.TabStop = false;
            groupBox3.Text = "Antivirus";
            // 
            // checkBoxKaspersky
            // 
            checkBoxKaspersky.Location = new Point(6, 22);
            checkBoxKaspersky.Name = "checkBoxKaspersky";
            checkBoxKaspersky.Size = new Size(104, 24);
            checkBoxKaspersky.TabIndex = 7;
            checkBoxKaspersky.Text = "Kaspersky";
            checkBoxKaspersky.UseVisualStyleBackColor = true;
            // 
            // checkBoxBitdefender
            // 
            checkBoxBitdefender.Location = new Point(6, 52);
            checkBoxBitdefender.Name = "checkBoxBitdefender";
            checkBoxBitdefender.Size = new Size(104, 24);
            checkBoxBitdefender.TabIndex = 8;
            checkBoxBitdefender.Text = "Bit Defender";
            checkBoxBitdefender.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(checkBoxHDDS);
            groupBox2.Controls.Add(checkBoxTreeSize);
            groupBox2.Location = new Point(660, 155);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(155, 334);
            groupBox2.TabIndex = 12;
            groupBox2.TabStop = false;
            groupBox2.Text = "Autres";
            // 
            // checkBoxHDDS
            // 
            checkBoxHDDS.Location = new Point(6, 52);
            checkBoxHDDS.Name = "checkBoxHDDS";
            checkBoxHDDS.Size = new Size(104, 24);
            checkBoxHDDS.TabIndex = 1;
            checkBoxHDDS.Text = "HDD Sentinel";
            checkBoxHDDS.UseVisualStyleBackColor = true;
            // 
            // checkBoxTreeSize
            // 
            checkBoxTreeSize.Location = new Point(6, 22);
            checkBoxTreeSize.Name = "checkBoxTreeSize";
            checkBoxTreeSize.Size = new Size(104, 24);
            checkBoxTreeSize.TabIndex = 0;
            checkBoxTreeSize.Text = "TreeSize";
            checkBoxTreeSize.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(checkBoxSublimeText);
            groupBox1.Controls.Add(checkBox7ZIP);
            groupBox1.Controls.Add(checkBoxTeams);
            groupBox1.Controls.Add(checkBoxVLC);
            groupBox1.Controls.Add(checkBoxAdobe);
            groupBox1.Controls.Add(checkBoxLibreOffice);
            groupBox1.Controls.Add(checkBoxChrome);
            groupBox1.Location = new Point(16, 155);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(316, 334);
            groupBox1.TabIndex = 11;
            groupBox1.TabStop = false;
            groupBox1.Text = "Setup de base";
            // 
            // checkBoxSublimeText
            // 
            checkBoxSublimeText.Location = new Point(6, 112);
            checkBoxSublimeText.Name = "checkBoxSublimeText";
            checkBoxSublimeText.Size = new Size(104, 24);
            checkBoxSublimeText.TabIndex = 6;
            checkBoxSublimeText.Text = "Sublime Text";
            checkBoxSublimeText.UseVisualStyleBackColor = true;
            // 
            // checkBox7ZIP
            // 
            checkBox7ZIP.Location = new Point(147, 82);
            checkBox7ZIP.Name = "checkBox7ZIP";
            checkBox7ZIP.Size = new Size(104, 24);
            checkBox7ZIP.TabIndex = 5;
            checkBox7ZIP.Text = "7ZIP";
            checkBox7ZIP.UseVisualStyleBackColor = true;
            // 
            // checkBoxTeams
            // 
            checkBoxTeams.Location = new Point(6, 82);
            checkBoxTeams.Name = "checkBoxTeams";
            checkBoxTeams.Size = new Size(104, 24);
            checkBoxTeams.TabIndex = 5;
            checkBoxTeams.Text = "Teams";
            checkBoxTeams.UseVisualStyleBackColor = true;
            // 
            // checkBoxVLC
            // 
            checkBoxVLC.AccessibleName = "checkBoxVLC";
            checkBoxVLC.Checked = true;
            checkBoxVLC.CheckState = CheckState.Checked;
            checkBoxVLC.Location = new Point(6, 22);
            checkBoxVLC.Name = "checkBoxVLC";
            checkBoxVLC.Size = new Size(104, 24);
            checkBoxVLC.TabIndex = 1;
            checkBoxVLC.Text = "VLC";
            checkBoxVLC.UseVisualStyleBackColor = true;
            // 
            // checkBoxAdobe
            // 
            checkBoxAdobe.AccessibleName = "checkBoxAdobe";
            checkBoxAdobe.Checked = true;
            checkBoxAdobe.CheckState = CheckState.Checked;
            checkBoxAdobe.Location = new Point(6, 52);
            checkBoxAdobe.Name = "checkBoxAdobe";
            checkBoxAdobe.Size = new Size(104, 24);
            checkBoxAdobe.TabIndex = 2;
            checkBoxAdobe.Text = "Adobe";
            checkBoxAdobe.UseVisualStyleBackColor = true;
            // 
            // checkBoxLibreOffice
            // 
            checkBoxLibreOffice.Checked = true;
            checkBoxLibreOffice.CheckState = CheckState.Checked;
            checkBoxLibreOffice.Location = new Point(147, 22);
            checkBoxLibreOffice.Name = "checkBoxLibreOffice";
            checkBoxLibreOffice.Size = new Size(104, 24);
            checkBoxLibreOffice.TabIndex = 3;
            checkBoxLibreOffice.Text = "Libre Office";
            checkBoxLibreOffice.UseVisualStyleBackColor = true;
            // 
            // checkBoxChrome
            // 
            checkBoxChrome.Checked = true;
            checkBoxChrome.CheckState = CheckState.Checked;
            checkBoxChrome.Location = new Point(147, 52);
            checkBoxChrome.Name = "checkBoxChrome";
            checkBoxChrome.Size = new Size(104, 24);
            checkBoxChrome.TabIndex = 4;
            checkBoxChrome.Text = "Chrome";
            checkBoxChrome.UseVisualStyleBackColor = true;
            // 
            // InstallSelection
            // 
            InstallSelection.Location = new Point(6, 6);
            InstallSelection.Name = "InstallSelection";
            InstallSelection.Size = new Size(202, 31);
            InstallSelection.TabIndex = 0;
            InstallSelection.Text = "Installer la selection";
            InstallSelection.UseVisualStyleBackColor = true;
            InstallSelection.Click += BtnOpenPowerShell_Click;
            // 
            // tabWinUpdate
            // 
            tabWinUpdate.Controls.Add(buttonWinUpdate);
            tabWinUpdate.Location = new Point(4, 24);
            tabWinUpdate.Name = "tabWinUpdate";
            tabWinUpdate.Padding = new Padding(3);
            tabWinUpdate.Size = new Size(889, 580);
            tabWinUpdate.TabIndex = 2;
            tabWinUpdate.Text = "Windows";
            tabWinUpdate.UseVisualStyleBackColor = true;
            // 
            // buttonWinUpdate
            // 
            buttonWinUpdate.Location = new Point(6, 6);
            buttonWinUpdate.Name = "buttonWinUpdate";
            buttonWinUpdate.Size = new Size(202, 31);
            buttonWinUpdate.TabIndex = 0;
            buttonWinUpdate.Text = "Windows Update";
            buttonWinUpdate.UseVisualStyleBackColor = true;
            buttonWinUpdate.Click += ButtonWinUpdate_Click;
            // 
            // tabOptimisation
            // 
            tabOptimisation.Controls.Add(groupBox7);
            tabOptimisation.Controls.Add(groupBox6);
            tabOptimisation.Location = new Point(4, 24);
            tabOptimisation.Name = "tabOptimisation";
            tabOptimisation.Padding = new Padding(3);
            tabOptimisation.Size = new Size(889, 580);
            tabOptimisation.TabIndex = 3;
            tabOptimisation.Text = "Optimisation";
            tabOptimisation.UseVisualStyleBackColor = true;
            // 
            // groupBox7
            // 
            groupBox7.Controls.Add(checkedListBoxServices);
            groupBox7.Controls.Add(buttonOptimisation);
            groupBox7.Location = new Point(485, 15);
            groupBox7.Name = "groupBox7";
            groupBox7.Size = new Size(396, 549);
            groupBox7.TabIndex = 3;
            groupBox7.TabStop = false;
            groupBox7.Text = "Services";
            // 
            // checkedListBoxServices
            // 
            checkedListBoxServices.FormattingEnabled = true;
            checkedListBoxServices.Location = new Point(6, 56);
            checkedListBoxServices.Name = "checkedListBoxServices";
            checkedListBoxServices.Size = new Size(384, 472);
            checkedListBoxServices.TabIndex = 2;
            // 
            // buttonOptimisation
            // 
            buttonOptimisation.Location = new Point(121, 22);
            buttonOptimisation.Name = "buttonOptimisation";
            buttonOptimisation.Size = new Size(166, 28);
            buttonOptimisation.TabIndex = 1;
            buttonOptimisation.Text = "Optimisation des services";
            buttonOptimisation.UseVisualStyleBackColor = true;
            buttonOptimisation.Click += BtnDisableSelectedServices_Click;
            // 
            // groupBox6
            // 
            groupBox6.Controls.Add(checkedListBoxApps);
            groupBox6.Controls.Add(buttonBloatWare);
            groupBox6.Location = new Point(12, 15);
            groupBox6.Name = "groupBox6";
            groupBox6.Size = new Size(396, 549);
            groupBox6.TabIndex = 2;
            groupBox6.TabStop = false;
            groupBox6.Text = "Bloatwares";
            // 
            // checkedListBoxApps
            // 
            checkedListBoxApps.FormattingEnabled = true;
            checkedListBoxApps.Location = new Point(6, 56);
            checkedListBoxApps.Name = "checkedListBoxApps";
            checkedListBoxApps.Size = new Size(384, 472);
            checkedListBoxApps.TabIndex = 1;
            // 
            // buttonBloatWare
            // 
            buttonBloatWare.Location = new Point(114, 22);
            buttonBloatWare.Name = "buttonBloatWare";
            buttonBloatWare.Size = new Size(166, 28);
            buttonBloatWare.TabIndex = 0;
            buttonBloatWare.Text = "Remove Bloatwares";
            buttonBloatWare.UseVisualStyleBackColor = true;
            buttonBloatWare.Click += BtnUninstallSelectedApps_Click;
            // 
            // _tabStress
            // 
            _tabStress.Controls.Add(groupBox9);
            _tabStress.Controls.Add(groupBox8);
            _tabStress.Controls.Add(buttonStressBOTH);
            _tabStress.Location = new Point(4, 24);
            _tabStress.Name = "_tabStress";
            _tabStress.Padding = new Padding(3);
            _tabStress.Size = new Size(889, 580);
            _tabStress.TabIndex = 4;
            _tabStress.Text = "Stress";
            _tabStress.UseVisualStyleBackColor = true;
            // 
            // groupBox9
            // 
            groupBox9.Controls.Add(_buttonStressGPU);
            groupBox9.Location = new Point(218, 16);
            groupBox9.Name = "groupBox9";
            groupBox9.Size = new Size(200, 100);
            groupBox9.TabIndex = 4;
            groupBox9.TabStop = false;
            groupBox9.Text = "GPU Stress";
            // 
            // _buttonStressGPU
            // 
            _buttonStressGPU.Location = new Point(6, 22);
            _buttonStressGPU.Name = "_buttonStressGPU";
            _buttonStressGPU.Size = new Size(141, 23);
            _buttonStressGPU.TabIndex = 1;
            _buttonStressGPU.Text = "Furmark 2";
            _buttonStressGPU.UseVisualStyleBackColor = true;
            _buttonStressGPU.Click += BtnStressGpu_Click;
            // 
            // groupBox8
            // 
            groupBox8.Controls.Add(_buttonStressCpu);
            groupBox8.Location = new Point(12, 16);
            groupBox8.Name = "groupBox8";
            groupBox8.Size = new Size(200, 100);
            groupBox8.TabIndex = 3;
            groupBox8.TabStop = false;
            groupBox8.Text = "CPU Stess";
            // 
            // _buttonStressCpu
            // 
            _buttonStressCpu.Location = new Point(6, 22);
            _buttonStressCpu.Name = "_buttonStressCpu";
            _buttonStressCpu.Size = new Size(141, 23);
            _buttonStressCpu.TabIndex = 0;
            _buttonStressCpu.Text = "_buttonStressCpu";
            _buttonStressCpu.UseVisualStyleBackColor = true;
            _buttonStressCpu.Click += BtnStressCpu_Click;
            // 
            // buttonStressBOTH
            // 
            buttonStressBOTH.Enabled = false;
            buttonStressBOTH.Location = new Point(131, 122);
            buttonStressBOTH.Name = "buttonStressBOTH";
            buttonStressBOTH.Size = new Size(180, 23);
            buttonStressBOTH.TabIndex = 2;
            buttonStressBOTH.Text = "buttonStressBOTH";
            buttonStressBOTH.UseVisualStyleBackColor = true;
            buttonStressBOTH.Click += BtnStressBoth_Click;
            // 
            // labelCpuTemp
            // 
            labelCpuTemp.AccessibleName = "labelCpuTemp";
            labelCpuTemp.Location = new Point(16, 611);
            labelCpuTemp.Name = "labelCpuTemp";
            labelCpuTemp.Size = new Size(100, 16);
            labelCpuTemp.TabIndex = 1;
            labelCpuTemp.Text = "CPU : -- °C";
            // 
            // labelGpuTemp
            // 
            labelGpuTemp.AccessibleName = "labelGpuTemp";
            labelGpuTemp.Location = new Point(16, 627);
            labelGpuTemp.Name = "labelGpuTemp";
            labelGpuTemp.Size = new Size(100, 16);
            labelGpuTemp.TabIndex = 2;
            labelGpuTemp.Text = "GPU : -- °C";
            // 
            // labelWindowsActivation
            // 
            labelWindowsActivation.Location = new Point(567, 620);
            labelWindowsActivation.Name = "labelWindowsActivation";
            labelWindowsActivation.Size = new Size(176, 23);
            labelWindowsActivation.TabIndex = 0;
            labelWindowsActivation.Text = "Windows :";
            // 
            // labelCPURef
            // 
            labelCPURef.Location = new Point(122, 611);
            labelCPURef.Name = "labelCPURef";
            labelCPURef.Size = new Size(439, 16);
            labelCPURef.TabIndex = 3;
            labelCPURef.Text = "CPU NOT FOUND";
            // 
            // labelGPURef
            // 
            labelGPURef.Location = new Point(122, 627);
            labelGPURef.Name = "labelGPURef";
            labelGPURef.Size = new Size(439, 16);
            labelGPURef.TabIndex = 4;
            labelGPURef.Text = "GPU NOT FOUND";
            // 
            // _buttonActivation
            // 
            _buttonActivation.Location = new Point(775, 616);
            _buttonActivation.Name = "_buttonActivation";
            _buttonActivation.Size = new Size(75, 23);
            _buttonActivation.TabIndex = 5;
            _buttonActivation.Text = "Activer";
            _buttonActivation.UseVisualStyleBackColor = true;
            _buttonActivation.Click += BtnActivateWindows_Click;
            // 
            // MainForm
            // 
            ClientSize = new Size(897, 649);
            Controls.Add(_buttonActivation);
            Controls.Add(labelGPURef);
            Controls.Add(labelCPURef);
            Controls.Add(labelWindowsActivation);
            Controls.Add(labelGpuTemp);
            Controls.Add(labelCpuTemp);
            Controls.Add(AllTabs);
            FormBorderStyle = FormBorderStyle.Fixed3D;
            Icon = ((Icon)resources.GetObject("$this.Icon"));
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Savior";
            Load += MainForm_Load;
            AllTabs.ResumeLayout(false);
            TabGeneral.ResumeLayout(false);
            groupBox10.ResumeLayout(false);
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
            _tabStress.ResumeLayout(false);
            groupBox9.ResumeLayout(false);
            groupBox8.ResumeLayout(false);
            ResumeLayout(false);
        }


        private Label label1;

        private Button buttonBasicInstallGeneral;

        private GroupBox groupBox10;

        private GroupBox groupBox8;
        private GroupBox groupBox9;


        private GroupBox groupBox6;
        private GroupBox groupBox7;

        private TabPage tabOptimisation;

        private TabPage tabWinUpdate;

        private GroupBox groupBox5;

        private GroupBox groupBox3;
        private GroupBox groupBox4;

        private GroupBox groupBox2;

        private GroupBox groupBox1;

        private Button InstallSelection;

        private TabControl AllTabs;
        private TabPage TabGeneral;
        private TabPage TabSoftwares;
    }
}