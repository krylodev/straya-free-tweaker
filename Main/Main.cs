using AutoUpdaterDotNET;
using Guna.UI2.WinForms;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel;
using IWshRuntimeLibrary;
using System.Reflection;

namespace strayafreetweakingutil
{
    public partial class Main : Form, ISerializable
    {
        // DLLs
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect,
            int nTopRect,
            int nRightRect,
            int nBottomRect,
            int nWidthEllipse,
            int nHeightEllipse
        );

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, int dwFlags);
        // DLLs end

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public static bool IsUpdateEnabled { get; private set; } = true;


        // main
        public Main()
        {
            InitializeComponent();
            Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 20, 20));
            if (!IsAdministrator()) {
                this.Close();
            }
        }

        protected Main(SerializationInfo info, StreamingContext context)
        {
            InitializeComponent();
            Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 20, 20));
            IsUpdateEnabled = info.GetBoolean(nameof(IsUpdateEnabled));
            if (!IsAdministrator()) {
                this.Close();
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(IsUpdateEnabled), IsUpdateEnabled);
        }

        private void CheckForUpdates()
        {
            if (IsUpdateEnabled) {
                AutoUpdater.Start("https://github.com/krylodev/straya-free-tweaker/raw/refs/heads/main/AutoUpdater.xml");
            }
            else {
                MessageBox.Show("Automatic updates are disabled.", "Update Disabled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void Main_Load(object sender, EventArgs e)
        {
            CheckForUpdates();

            desktopshortcu.PerformClick();

            TweaksButton.Checked = true;
            TweaksPanel.Show();
            FixesButton.Checked = false;
            FixPanel.Hide();
            SettingsButton.Checked = false;
            SettingsPanel.Hide();

        }
        // main end

        // app performance
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams handleParam = base.CreateParams;
                handleParam.ExStyle |= 0x02000000;
                return handleParam;
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Color backgroundColor = Color.FromArgb(19, 18, 21);
            e.Graphics.Clear(backgroundColor);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0014) {
                m.Result = IntPtr.Zero;
                return;
            }
            base.WndProc(ref m);
        }
        // app performance end

        // functions
        private bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private async Task BatchCommands(string commands)
        {
            var processInfo = new ProcessStartInfo("cmd.exe")
            {
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = false
            };

            try {
                using (var process = Process.Start(processInfo)) {
                    if (process != null) {
                        using (var standardInput = process.StandardInput) {
                            if (standardInput.BaseStream.CanWrite) {
                                await standardInput.WriteLineAsync(commands);
                                await standardInput.FlushAsync();
                            }
                            standardInput.Close();
                            await process.WaitForExitAsync();
                        }
                    }
                }
            }
            catch (Exception ex) {
                MessageBox.Show("error: " + ex.Message);
            }
        }

        public async Task PowerShellCommands(string commands)
        {
            var processInfo = new ProcessStartInfo("powershell.exe")
            {
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = false
            };

            try {
                using (var process = Process.Start(processInfo)) {
                    if (process != null) {
                        using (var standardInput = process.StandardInput) {
                            if (standardInput.BaseStream.CanWrite) {
                                await standardInput.WriteLineAsync(commands);
                                await standardInput.FlushAsync();
                            }
                            standardInput.Close();
                            await process.WaitForExitAsync();
                        }
                    }
                }
            }
            catch (Exception ex) {
                MessageBox.Show("error: " + ex.Message);
            }
        }
        // functions end

        // sidebar buttons
        private void TweaksButton_Click(object sender, EventArgs e)
        {
            TweaksButton.Checked = true;
            TweaksPanel.Show();
            FixesButton.Checked = false;
            FixPanel.Hide();
            SettingsButton.Checked = false;
            SettingsPanel.Hide();
        }

        private void FixesButton_Click(object sender, EventArgs e)
        {
            TweaksButton.Checked = false;
            TweaksPanel.Hide();
            FixesButton.Checked = true;
            FixPanel.Show();
            SettingsButton.Checked = false;
            SettingsPanel.Hide();
        }

        private void SettingsButton_Click(object sender, EventArgs e)
        {
            TweaksButton.Checked = false;
            TweaksPanel.Hide();
            FixesButton.Checked = false;
            FixPanel.Hide();
            SettingsButton.Checked = true;
            SettingsPanel.Show();
        }
        // sidebar buttons end


        // Tweaks Tab
        private void windowstweaks_CheckedChanged(object sender, EventArgs e)
        {
            if (windowstweaks.Checked) {
                _ = WindowsTweaks();
            }
        }

        private async Task WindowsTweaks()
        {
            string commands = @"
Auditpol /set /subcategory:""Process Termination"" /success:disable /failure:enable
Auditpol /set /subcategory:""RPC Events"" /success:disable /failure:enable
Auditpol /set /subcategory:""Filtering Platform Connection"" /success:disable /failure:enable
Auditpol /set /subcategory:""DPAPI Activity"" /success:disable /failure:disable
Auditpol /set /subcategory:""IPsec Driver"" /success:disable /failure:enable
Auditpol /set /subcategory:""Other System Events"" /success:disable /failure:enable
Auditpol /set /subcategory:""Security State Change"" /success:disable /failure:enable
Auditpol /set /subcategory:""Security System Extension"" /success:disable /failure:enable
Auditpol /set /subcategory:""System Integrity"" /success:disable /failure:enable
Reg.exe add ""HKLM\System\CurrentControlSet\Control\WMI\Autologger\AutoLogger-Diagtrack-Listener"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\System\CurrentControlSet\Control\WMI\Autologger\DiagLog"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\System\CurrentControlSet\Control\WMI\Autologger\Diagtrack-Listener"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\System\CurrentControlSet\Control\WMI\Autologger\WiFiSession"" /v ""Start"" /t REG_DWORD /d ""0"" /f
echo %w% - Track Only Important Failure Events %b%
Auditpol /set /subcategory:""Process Termination"" /success:disable /failure:enable
Auditpol /set /subcategory:""RPC Events"" /success:disable /failure:enable
Auditpol /set /subcategory:""Filtering Platform Connection"" /success:disable /failure:enable
Auditpol /set /subcategory:""DPAPI Activity"" /success:disable /failure:disable
Auditpol /set /subcategory:""IPsec Driver"" /success:disable /failure:enable
Auditpol /set /subcategory:""Other System Events"" /success:disable /failure:enable
Auditpol /set /subcategory:""Security State Change"" /success:disable /failure:enable
Auditpol /set /subcategory:""Security System Extension"" /success:disable /failure:enable
Auditpol /set /subcategory:""System Integrity"" /success:disable /failure:enable
Reg.exe add ""HKLM\System\CurrentControlSet\Control\WMI\Autologger\AutoLogger-Diagtrack-Listener"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\System\CurrentControlSet\Control\WMI\Autologger\DiagLog"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\System\CurrentControlSet\Control\WMI\Autologger\Diagtrack-Listener"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\System\CurrentControlSet\Control\WMI\Autologger\WiFiSession"" /v ""Start"" /t REG_DWORD /d ""0"" /f
schtasks /end /tn ""\Microsoft\Windows\Customer Experience Improvement Program\Consolidator"" 
schtasks /change /tn ""\Microsoft\Windows\Customer Experience Improvement Program\Consolidator"" /disable 
schtasks /end /tn ""\Microsoft\Windows\Customer Experience Improvement Program\BthSQM"" 
schtasks /change /tn ""\Microsoft\Windows\Customer Experience Improvement Program\BthSQM"" /disable 
schtasks /end /tn ""\Microsoft\Windows\Customer Experience Improvement Program\KernelCeipTask"" 
schtasks /change /tn ""\Microsoft\Windows\Customer Experience Improvement Program\KernelCeipTask"" /disable 
schtasks /end /tn ""\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip"" 
schtasks /change /tn ""\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip"" /disable 
schtasks /end /tn ""\Microsoft\Windows\Customer Experience Improvement Program\Uploader"" 
schtasks /change /tn ""\Microsoft\Windows\Customer Experience Improvement Program\Uploader"" /disable 
schtasks /end /tn ""\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser"" 
schtasks /change /tn ""\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser"" /disable 
schtasks /end /tn ""\Microsoft\Windows\Application Experience\ProgramDataUpdater"" 
schtasks /change /tn ""\Microsoft\Windows\Application Experience\ProgramDataUpdater"" /disable 
schtasks /end /tn ""\Microsoft\Windows\Application Experience\StartupAppTask"" 
schtasks /change /tn ""\Microsoft\Windows\Application Experience\StartupAppTask"" /disable 
schtasks /end /tn ""\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector"" 
schtasks /change /tn ""\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector"" /disable 
schtasks /end /tn ""\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticResolver"" 
schtasks /change /tn ""\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticResolver"" /disable 
schtasks /end /tn ""\Microsoft\Windows\Power Efficiency Diagnostics\AnalyzeSystem"" 
schtasks /change /tn ""\Microsoft\Windows\Power Efficiency Diagnostics\AnalyzeSystem"" /disable 
schtasks /end /tn ""\Microsoft\Windows\Shell\FamilySafetyMonitor"" 
schtasks /change /tn ""\Microsoft\Windows\Shell\FamilySafetyMonitor"" /disable 
schtasks /end /tn ""\Microsoft\Windows\Shell\FamilySafetyRefresh"" 
schtasks /change /tn ""\Microsoft\Windows\Shell\FamilySafetyRefresh"" /disable 
schtasks /end /tn ""\Microsoft\Windows\Shell\FamilySafetyUpload"" 
schtasks /change /tn ""\Microsoft\Windows\Shell\FamilySafetyUpload"" /disable 
schtasks /end /tn ""\Microsoft\Windows\Autochk\Proxy"" 
schtasks /change /tn ""\Microsoft\Windows\Autochk\Proxy"" /disable 
schtasks /end /tn ""\Microsoft\Windows\Maintenance\WinSAT"" 
schtasks /change /tn ""\Microsoft\Windows\Maintenance\WinSAT"" /disable 
schtasks /end /tn ""\Microsoft\Windows\Application Experience\AitAgent"" 
schtasks /change /tn ""\Microsoft\Windows\Application Experience\AitAgent"" /disable 
schtasks /end /tn ""\Microsoft\Windows\Windows Error Reporting\QueueReporting"" 
schtasks /change /tn ""\Microsoft\Windows\Windows Error Reporting\QueueReporting"" /disable 
schtasks /end /tn ""\Microsoft\Windows\CloudExperienceHost\CreateObjectTask"" 
schtasks /change /tn ""\Microsoft\Windows\CloudExperienceHost\CreateObjectTask"" /disable 
schtasks /end /tn ""\Microsoft\Windows\DiskFootprint\Diagnostics"" 
schtasks /change /tn ""\Microsoft\Windows\DiskFootprint\Diagnostics"" /disable 
schtasks /end /tn ""\Microsoft\Windows\PI\Sqm-Tasks"" 
schtasks /change /tn ""\Microsoft\Windows\PI\Sqm-Tasks"" /disable 
schtasks /end /tn ""\Microsoft\Windows\NetTrace\GatherNetworkInfo"" 
schtasks /change /tn ""\Microsoft\Windows\NetTrace\GatherNetworkInfo"" /disable 
schtasks /end /tn ""\Microsoft\Windows\AppID\SmartScreenSpecific"" 
schtasks /change /tn ""\Microsoft\Windows\AppID\SmartScreenSpecific"" /disable 
schtasks /end /tn ""\Microsoft\Office\OfficeTelemetryAgentFallBack2016"" 
schtasks /change /tn ""\Microsoft\Office\OfficeTelemetryAgentFallBack2016"" /disable 
schtasks /end /tn ""\Microsoft\Office\OfficeTelemetryAgentLogOn2016"" 
schtasks /change /tn ""\Microsoft\Office\OfficeTelemetryAgentLogOn2016"" /disable 
schtasks /end /tn ""\Microsoft\Office\OfficeTelemetryAgentLogOn"" 
schtasks /change /TN ""\Microsoft\Office\OfficeTelemetryAgentLogOn"" /disable 
schtasks /end /tn ""\Microsoftd\Office\OfficeTelemetryAgentFallBack"" 
schtasks /change /TN ""\Microsoftd\Office\OfficeTelemetryAgentFallBack"" /disable 
schtasks /end /tn ""\Microsoft\Office\Office 15 Subscription Heartbeat"" 
schtasks /change /TN ""\Microsoft\Office\Office 15 Subscription Heartbeat"" /disable 
schtasks /end /tn ""\Microsoft\Windows\Time Synchronization\ForceSynchronizeTime"" 
schtasks /change /TN ""\Microsoft\Windows\Time Synchronization\ForceSynchronizeTime"" /disable 
schtasks /end /tn ""\Microsoft\Windows\Time Synchronization\SynchronizeTime"" 
schtasks /change /TN ""\Microsoft\Windows\Time Synchronization\SynchronizeTime"" /disable 
schtasks /end /tn ""\Microsoft\Windows\WindowsUpdate\Automatic App Update"" 
schtasks /change /TN ""\Microsoft\Windows\WindowsUpdate\Automatic App Update"" /disable 
schtasks /end /tn ""\Microsoft\Windows\Device Information\Device"" 
schtasks /change /TN ""\Microsoft\Windows\Device Information\Device"" /disable 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\AppModel"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\Cellcore"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\Circular Kernel Context Logger"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\CloudExperienceHostOobe"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\DataMarket"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\DefenderApiLogger"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\DefenderAuditLogger"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\DiagLog"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\HolographicDevice"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\iclsClient"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\iclsProxy"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\LwtNetLog"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\Mellanox-Kernel"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\Microsoft-Windows-AssignedAccess-Trace"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\Microsoft-Windows-Setup"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\NBSMBLOGGER"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\PEAuthLog"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\RdrLog"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\ReadyBoot"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\SetupPlatform"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\SetupPlatformTel"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\SocketHeciServer"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\SpoolerLogger"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\SQMLogger"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\TCPIPLOGGER"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\TileStore"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\Tpm"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\TPMProvisioningService"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\UBPM"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\WdiContextLog"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\WFP-IPsec Trace"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\WiFiDriverIHVSession"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\WiFiDriverIHVSessionRepro"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\WiFiSession"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\WinPhoneCritical"" /v ""Start"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\WUDF"" /v ""LogEnable"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\WUDF"" /v ""LogLevel"" /t REG_DWORD /d ""0"" /f  
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent"" /v ""DisableThirdPartySuggestions"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent"" /v ""DisableWindowsConsumerFeatures"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Lsa\Credssp"" /v ""DebugLogLevel"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKLM\SOFTWARE\Microsoft\PolicyManager\current\device\System"" /v ""AllowExperimentation"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKLM\SOFTWARE\Microsoft\PolicyManager\default\System\AllowExperimentation"" /v ""value"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Feeds"" /v ""EnableFeeds"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft"" /v ""AllowNewsAndInterests"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\System"" /v ""EnableActivityFeed"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Control Panel\International\User Profile"" /v ""HttpAcceptLanguageOptOut"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo"" /v ""Enabled"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\Software\Policies\Microsoft\Windows\System"" /v ""EnableActivityFeed"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance"" /v ""MaintenanceDisabled"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\PushNotifications"" /v ""ToastEnabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings"" /v ""NOC_GLOBAL_SETTING_ALLOW_NOTIFICATION_SOUND"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings"" /v ""NOC_GLOBAL_SETTING_ALLOW_CRITICAL_TOASTS_ABOVE_LOCK"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\QuietHours"" /v ""Enabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\windows.immersivecontrolpanel_cw5n1h2txyewy!microsoft.windows.immersivecontrolpanel"" /v ""Enabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.AutoPlay"" /v ""Enabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.LowDisk"" /v ""Enabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.Print.Notification"" /v ""Enabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.SecurityAndMaintenance"" /v ""Enabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.WiFiNetworkManager"" /v ""Enabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Policies\Microsoft\Windows\Explorer"" /v ""DisableNotificationCenter"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Services\BTAGService"" /v ""Start"" /t REG_DWORD /d ""4"" /f
Reg.exe add ""HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Services\bthserv"" /v ""Start"" /t REG_DWORD /d ""4"" /f
Reg.exe add ""HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Services\BthAvctpSvc"" /v ""Start"" /t REG_DWORD /d ""4"" /f
Reg.exe add ""HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Services\BluetoothUserService"" /v ""Start"" /t REG_DWORD /d ""4"" /f
sc stop DiagTrack > nul 2>&1
sc config DiagTrack start= disabled > nul 2>&1
sc stop dmwappushservice > nul 2>&1
sc config dmwappushservice start= disabled > nul 2>&1
sc stop diagnosticshub.standardcollector.service > nul 2>&1
sc config diagnosticshub.standardcollector.service start= disabled > nul 2>&1
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting"" /v ""Disabled"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting"" /v ""DoReport"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting"" /v ""LoggingDisabled"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\PCHealth\ErrorReporting"" /v ""DoReport"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows\Windows Error Reporting"" /v ""Disabled"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\CDP"" /v ""CdpSessionUserAuthzPolicy"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\CDP"" /v ""NearShareChannelUserAuthzPolicy"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync\Groups\Accessibility"" /v ""Enabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync\Groups\AppSync"" /v ""Enabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync\Groups\BrowserSettings"" /v ""Enabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync\Groups\Credentials"" /v ""Enabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync\Groups\DesktopTheme"" /v ""Enabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync\Groups\Language"" /v ""Enabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync\Groups\PackageState"" /v ""Enabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync\Groups\Personalization"" /v ""Enabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync\Groups\StartLayout"" /v ""Enabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync\Groups\Windows"" /v ""Enabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\System"" /v ""EnableSmartScreen"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer"" /v ""SmartScreenEnabled"" /t REG_SZ /d ""Off"" /f 
Reg.exe add ""HKU\!USER_SID!\SOFTWARE\Microsoft\Windows\CurrentVersion\AppHost"" /v ""EnableWebContentEvaluation"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v ""PreInstalledAppsEnabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v ""SilentInstalledAppsEnabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v ""OemPreInstalledAppsEnabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v ""ContentDeliveryAllowed"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v ""SubscribedContentEnabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v ""PreInstalledAppsEverEnabled"" /t REG_DWORD /d ""0"" 
Reg.exe add ""HKU\!USER_SID!\SOFTWARE\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications"" /v ""GlobalUserDisabled"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKU\!USER_SID!\SOFTWARE\Microsoft\Windows\CurrentVersion\Search"" /v ""BackgroundAppGlobalToggle"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\bam"" /v ""Start"" /t REG_DWORD /d ""4"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\dam"" /v ""Start"" /t REG_DWORD /d ""4"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize"" /v ""EnableTransparency"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\SOFTWARE\Microsoft\GameBar"" /v ""AllowAutoGameMode"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\GameBar"" /v ""AutoGameModeEnabled"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Privacy"" /v ""TailoredExperiencesWithDiagnosticDataEnabled"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Diagnostics\DiagTrack"" /v ""ShowedToastAtLevel"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKEY_CURRENT_USER\Software\Microsoft\Input\TIPC"" /v ""Enabled"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\System"" /v ""UploadUserActivities"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\System"" /v ""PublishUserActivities"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKEY_CURRENT_USER\Control Panel\International\User Profile"" /v ""HttpAcceptLanguageOptOut"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Policies\Attachments"" /v ""SaveZoneInformation"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\Diagnostics\Performance"" /v ""DisableDiagnosticTracing"" /t REG_DWORD /d ""1"" /f >nul 2>&1 
Reg.exe add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\WDI\{9c5a40da-b965-4fc3-8781-88dd50a6299d}"" /v ""ScenarioExecutionEnabled"" /t REG_DWORD /d ""0"" /f
schtasks /change /tn ""\Microsoft\Windows\Application Experience\StartupAppTask"" /disable
schtasks /end /tn ""\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector""
schtasks /change /tn ""\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector"" /disable
schtasks /end /tn ""\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticResolver""
schtasks /change /tn ""\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticResolver"" /disable
schtasks /end /tn ""\Microsoft\Windows\Power Efficiency Diagnostics\AnalyzeSystem""
schtasks /change /tn ""\Microsoft\Windows\Power Efficiency Diagnostics\AnalyzeSystem"" /disable
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v ""PreInstalledAppsEnabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v ""SilentInstalledAppsEnabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v ""OemPreInstalledAppsEnabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v ""ContentDeliveryAllowed"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v ""SubscribedContentEnabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v ""PreInstalledAppsEverEnabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKU\!USER_SID!\SOFTWARE\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications"" /v ""GlobalUserDisabled"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKU\!USER_SID!\SOFTWARE\Microsoft\Windows\CurrentVersion\Search"" /v ""BackgroundAppGlobalToggle"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\bam"" /v ""Start"" /t REG_DWORD /d ""4"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\dam"" /v ""Start"" /t REG_DWORD /d ""4"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v ""SystemPaneSuggestionsEnabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v ""SubscribedContent-338388Enabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v ""SubscribedContent-314559Enabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v ""SubscribedContent-280815Enabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v ""SubscribedContent-314563Enabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v ""SubscribedContent-338393Enabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v ""SubscribedContent-353694Enabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v ""SubscribedContent-353696Enabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v ""SubscribedContent-310093Enabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v ""SubscribedContent-202914Enabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v ""SubscribedContent-338387Enabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v ""SubscribedContent-338389Enabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v ""SubscribedContent-353698Enabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKU\!USER_SID!\SOFTWARE\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications"" /v ""GlobalUserDisabled"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKU\!USER_SID!\SOFTWARE\Microsoft\Windows\CurrentVersion\Search"" /v ""BackgroundAppGlobalToggle"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\bam"" /v ""Start"" /t REG_DWORD /d ""4"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\dam"" /v ""Start"" /t REG_DWORD /d ""4"" /f
        ";
            await Task.Run(() => BatchCommands(commands));
        }

        private void generaltweaks_CheckedChanged(object sender, EventArgs e)
        {
            if (generaltweaks.Checked) {
                _ = GeneralTweaks();
            }
        }

        private async Task GeneralTweaks()
        {
            string commands = @"
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Search"" /v ""BingSearchEnabled"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\InputPersonalization"" /v ""RestrictImplicitInkCollection"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\Software\Microsoft\InputPersonalization"" /v ""RestrictImplicitTextCollection"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\Software\Microsoft\Personalization\Settings"" /v ""AcceptedPrivacyPolicy"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Search"" /v ""CortanaCapabilities"" /t REG_SZ /d """" /f
Reg.exe add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Search"" /v ""IsAssignedAccess"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Search"" /v ""IsWindowsHelloActive"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v ""AllowSearchToUseLocation"" /t REG_DWORD /d 0 /f
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v ""ConnectedSearchPrivacy"" /t REG_DWORD /d 3 /f
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v ""ConnectedSearchSafeSearch"" /t REG_DWORD /d 3 /f
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v ""ConnectedSearchUseWeb"" /t REG_DWORD /d 0 /f
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v ""ConnectedSearchUseWebOverMeteredConnections"" /t REG_DWORD /d 0 /f
Reg.exe add ""HKLM\Software\Microsoft\PolicyManager\default\Experience\AllowCortana"" /v ""value"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\Software\Policies\Microsoft\SearchCompanion"" /v ""DisableContentFileUpdates"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\Software\Policies\Microsoft\Windows\Windows Search"" /v ""AllowCloudSearch"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\Software\Policies\Microsoft\Windows\Windows Search"" /v ""AllowCortanaAboveLock"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\Software\Policies\Microsoft\Windows\Windows Search"" /v ""AllowSearchToUseLocation"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\Software\Policies\Microsoft\Windows\Windows Search"" /v ""ConnectedSearchPrivacy"" /t REG_DWORD /d ""3"" /f
Reg.exe add ""HKLM\Software\Policies\Microsoft\Windows\Windows Search"" /v ""ConnectedSearchUseWeb"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\Software\Policies\Microsoft\Windows\Windows Search"" /v ""ConnectedSearchUseWebOverMeteredConnections"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\Software\Policies\Microsoft\Windows\Windows Search"" /v ""DisableWebSearch"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\Software\Policies\Microsoft\Windows\Windows Search"" /v ""DoNotUseWebResults"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Games"" /v ""FpsAll"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Games"" /v ""FpsStatusGames"" /t REG_DWORD /d ""10"" /f
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Games"" /v ""FpsStatusGamesAll"" /t REG_DWORD /d ""4"" /f
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Games"" /v ""GameFluidity"" /t REG_DWORD /d ""1"" /f
bcdedit /set disabledynamictick yes >nul 2>&1
bcdedit /deletevalue useplatformclock  >nul 2>&1
bcdedit /set useplatformtick yes  >nul 2>&1
fsutil behavior set memoryusage 2 >nul 2>&1
fsutil behavior set mftzone 4 >nul 2>&1
fsutil behavior set disablelastaccess 1 >nul 2>&1
fsutil behavior set disabledeletenotify 0 >nul 2>&1
fsutil behavior set encryptpagingfile 0 >nul 2>&1
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Low Latency"" /v ""Affinity"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Low Latency"" /v ""Background Only"" /t REG_SZ /d ""False"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Low Latency"" /v ""BackgroundPriority"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Low Latency"" /v ""Clock Rate"" /t REG_DWORD /d ""10000"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Low Latency"" /v ""GPU Priority"" /t REG_DWORD /d ""8"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Low Latency"" /v ""Priority"" /t REG_DWORD /d ""2"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Low Latency"" /v ""Scheduling Category"" /t REG_SZ /d ""Medium"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Low Latency"" /v ""SFIO Priority"" /t REG_SZ /d ""High"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Low Latency"" /v ""Latency Sensitive"" /t REG_SZ /d ""True"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"" /v ""Affinity"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"" /v ""Background Only"" /t REG_SZ /d ""False"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"" /v ""BackgroundPriority"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"" /v ""Clock Rate"" /t REG_DWORD /d ""10000"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"" /v ""GPU Priority"" /t REG_DWORD /d ""8"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"" /v ""Priority"" /t REG_DWORD /d ""2"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"" /v ""Scheduling Category"" /t REG_SZ /d ""High"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"" /v ""SFIO Priority"" /t REG_SZ /d ""High"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"" /v ""Latency Sensitive"" /t REG_SZ /d ""True"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl"" /v ""Win32PrioritySeparation"" /t REG_DWORD /d ""38"" /f 
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\DeviceGuard"" /v ""EnableVirtualizationBasedSecurity"" /t REG_DWORD /d ""0"" /f 
echo %w% - Disabling HVCIMATRequired%b%
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\DeviceGuard"" /v ""HVCIMATRequired"" /t REG_DWORD /d ""0"" /f 
echo %w% - Disabling ExceptionChainValidation%b%
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel"" /v ""DisableExceptionChainValidation"" /t REG_DWORD /d ""1"" /f 
echo %w% - Disabling Sehop%b%
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel"" /v ""KernelSEHOPEnabled"" /t REG_DWORD /d ""0"" /f 
echo %w% - Disabling CFG%b%
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v ""EnableCfg"" /t REG_DWORD /d ""0"" /f 
echo %w% - Disabling Protection Mode%b%
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager"" /v ""ProtectionMode"" /t REG_DWORD /d ""0"" /f 
echo %w% - Disabling Spectre And Meltdown%b%
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v ""FeatureSettings"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v ""FeatureSettingsOverride"" /t REG_DWORD /d ""3"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v ""FeatureSettingsOverrideMask"" /t REG_DWORD /d ""3"" /f 
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Reliability"" /v ""TimeStampInterval"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Reliability"" /v ""IoPriority"" /t REG_DWORD /d ""3"" /f 
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\csrss.exe\PerfOptions"" /v ""CpuPriorityClass"" /t REG_DWORD /d ""4"" /f 
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\csrss.exe\PerfOptions"" /v ""IoPriority"" /t REG_DWORD /d ""3"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\DXGKrnl"" /v ""MonitorLatencyTolerance"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\DXGKrnl"" /v ""MonitorRefreshLatencyTolerance"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""ExitLatency"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""ExitLatencyCheckEnabled"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""Latency"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""LatencyToleranceDefault"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""LatencyToleranceFSVP"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""LatencyTolerancePerfOverride"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""LatencyToleranceScreenOffIR"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""LatencyToleranceVSyncEnabled"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""RtlCapabilityCheckLatency"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Power"" /v ""DefaultD3TransitionLatencyActivelyUsed"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Power"" /v ""DefaultD3TransitionLatencyIdleLongTime"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Power"" /v ""DefaultD3TransitionLatencyIdleMonitorOff"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Power"" /v ""DefaultD3TransitionLatencyIdleNoContext"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Power"" /v ""DefaultD3TransitionLatencyIdleShortTime"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Power"" /v ""DefaultD3TransitionLatencyIdleVeryLongTime"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Power"" /v ""DefaultLatencyToleranceIdle0"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Power"" /v ""DefaultLatencyToleranceIdle0MonitorOff"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Power"" /v ""DefaultLatencyToleranceIdle1"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Power"" /v ""DefaultLatencyToleranceIdle1MonitorOff"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Power"" /v ""DefaultLatencyToleranceMemory"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Power"" /v ""DefaultLatencyToleranceNoContext"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Power"" /v ""DefaultLatencyToleranceNoContextMonitorOff"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Power"" /v ""DefaultLatencyToleranceOther"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Power"" /v ""DefaultLatencyToleranceTimerPeriod"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Power"" /v ""DefaultMemoryRefreshLatencyToleranceActivelyUsed"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Power"" /v ""DefaultMemoryRefreshLatencyToleranceMonitorOff"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Power"" /v ""DefaultMemoryRefreshLatencyToleranceNoContext"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Power"" /v ""Latency"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Power"" /v ""MaxIAverageGraphicsLatencyInOneBucket"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Power"" /v ""MiracastPerfTrackGraphicsLatency"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Power"" /v ""MonitorLatencyTolerance"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Power"" /v ""MonitorRefreshLatencyTolerance"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Power"" /v ""TransitionLatency"" /t REG_DWORD /d ""1"" /f 
        ";
            await Task.Run(() => BatchCommands(commands));
        }

        private void usbtweaks_CheckedChanged(object sender, EventArgs e)
        {
            if (usbtweaks.Checked) {
                _ = UsbTweaks();
            }
        }

        private async Task UsbTweaks()
        {
            string commands = @"
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Enum\%%i\Device Parameters"" /v ""AllowIdleIrpInD3"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Enum\%%i\Device Parameters"" /v ""D3ColdSupported"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Enum\%%i\Device Parameters"" /v ""DeviceSelectiveSuspended"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Enum\%%i\Device Parameters"" /v ""EnableSelectiveSuspend"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Enum\%%i\Device Parameters"" /v ""EnhancedPowerManagementEnabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Enum\%%i\Device Parameters"" /v ""SelectiveSuspendEnabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Enum\%%i\Device Parameters"" /v ""SelectiveSuspendOn"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\usbxhci\Parameters"" /v ""ThreadPriority"" /t REG_DWORD /d ""31"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\USBHUB3\Parameters"" /v ""ThreadPriority"" /t REG_DWORD /d ""31"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\nvlddmkm\Parameters"" /v ""ThreadPriority"" /t REG_DWORD /d ""31"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\NDIS\Parameters"" /v ""ThreadPriority"" /t REG_DWORD /d ""31"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\USB"" /v ""DisableSelectiveSuspend"" /t REG_DWORD /d ""1"" /f 
Reg.exe add ""HKLM\System\CurrentControlSet\Enum\%%i\Device Parameters\Interrupt Management\Affinity Policy"" /v ""DevicePriority"" /f
Reg.exe add ""HKLM\System\CurrentControlSet\Enum\%%i\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties"" /v ""MSISupported"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\Control Panel\Accessibility\Keyboard Response"" /v ""Flags"" /t REG_SZ /d ""122"" /f
Reg.exe add ""HKCU\Control Panel\Accessibility\ToggleKeys"" /v ""Flags"" /t REG_SZ /d ""58"" /f
Reg.exe add ""HKCU\Control Panel\Accessibility\StickyKeys"" /v ""Flags"" /t REG_SZ /d ""506"" /f
Reg.exe add ""HKCU\Control Panel\Accessibility\MouseKeys"" /v ""Flags"" /t REG_SZ /d ""0"" /f
Reg.exe add ""HKCU\Control Panel\Mouse"" /v ""MouseSpeed"" /t REG_SZ /d ""0"" /f
Reg.exe add ""HKCU\Control Panel\Mouse"" /v ""MouseThreshold1"" /t REG_SZ /d ""0"" /f
Reg.exe add ""HKCU\Control Panel\Mouse"" /v ""MouseThreshold2"" /t REG_SZ /d ""0"" /f
Reg.exe add ""HKCU\Control Panel\Mouse"" /v ""MouseSensitivity"" /t REG_SZ /d ""10"" /f
Reg.exe add ""HKCU\Control Panel\Keyboard"" /v ""KeyboardDelay"" /t REG_SZ /d ""0"" /f
Reg.exe add ""HKCU\Control Panel\Keyboard"" /v ""KeyboardSpeed"" /t REG_SZ /d ""31"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\csrss.exe\PerfOptions"" /v ""CpuPriorityClass"" /t REG_DWORD /d ""4"" /f 
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\csrss.exe\PerfOptions"" /v ""IoPriority"" /t REG_DWORD /d ""3"" /f 
        ";
            await Task.Run(() => BatchCommands(commands));
        }

        private void networktweaks_CheckedChanged(object sender, EventArgs e)
        {
            if (networktweaks.Checked) {
                _ = NetworkTweaks();
            }
        }

        private async Task NetworkTweaks()
        {
            string commands = @"
powershell -Command ""Write-Host 'This might take awhile, as the changes suppressed and nulled.' -ForegroundColor White -BackgroundColor Red""
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'Flow Control' | Set-NetAdapterAdvancedProperty -RegistryValue 0"" >nul 2>&1
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'Gigabit Master Slave Mode' | Set-NetAdapterAdvancedProperty -RegistryValue 0"" >nul 2>&1
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'IPv4 Checksum Offload' | Set-NetAdapterAdvancedProperty -RegistryValue 0"" >nul 2>&1
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'Jumbo Packet' | Set-NetAdapterAdvancedProperty -RegistryValue 0"" >nul 2>&1
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'Large Send Offload V2 (IPv4)' | Set-NetAdapterAdvancedProperty -RegistryValue 0"" >nul 2>&1
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'Large Send Offload V2 (IPv6)' | Set-NetAdapterAdvancedProperty -RegistryValue 0"" >nul 2>&1
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'Log Link State Event' | Set-NetAdapterAdvancedProperty -RegistryValue 16"" >nul 2>&1
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'Maximum Number of RSS Queues' | Set-NetAdapterAdvancedProperty -RegistryValue 4"" >nul 2>&1
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'Packet Priority & VLAN' | Set-NetAdapterAdvancedProperty -RegistryValue 0"" >nul 2>&1
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'Receive Buffers' | Set-NetAdapterAdvancedProperty -RegistryValue 512"" >nul 2>&1
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'TCP Checksum Offload (IPv4)' | Set-NetAdapterAdvancedProperty -RegistryValue 0"" >nul 2>&1
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'TCP Checksum Offload (IPv6)' | Set-NetAdapterAdvancedProperty -RegistryValue 0"" >nul 2>&1
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'Transmit Buffers' | Set-NetAdapterAdvancedProperty -RegistryValue 512"" >nul 2>&1
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'UDP Checksum Offload (IPv4)' | Set-NetAdapterAdvancedProperty -RegistryValue 0"" >nul 2>&1
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'UDP Checksum Offload (IPv6)' | Set-NetAdapterAdvancedProperty -RegistryValue 0"" >nul 2>&1
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'Wait for Link' | Set-NetAdapterAdvancedProperty -RegistryValue 0"" >nul 2>&1
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'Advanced EEE' | Set-NetAdapterAdvancedProperty -RegistryValue 0"" >nul 2>&1
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'ARP Offload' | Set-NetAdapterAdvancedProperty -RegistryValue 1"" >nul 2>&1
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'Energy-Efficent Ethernet' | Set-NetAdapterAdvancedProperty -RegistryValue 0"" >nul 2>&1
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'Gitabit Lite' | Set-NetAdapterAdvancedProperty -RegistryValue 0"" >nul 2>&1
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'Green Ethernet' | Set-NetAdapterAdvancedProperty -RegistryValue 0"">nul 2>&1
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'NS Offload' | Set-NetAdapterAdvancedProperty -RegistryValue 0"" >nul 2>&1
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'Power Saving Mode' | Set-NetAdapterAdvancedProperty -RegistryValue 0"" >nul 2>&1
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'Receive Side Scaling' | Set-NetAdapterAdvancedProperty -RegistryValue 1"" >nul 2>&1
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'Shutdown Wake-On-Lan' | Set-NetAdapterAdvancedProperty -RegistryValue 0"" >nul 2>&1
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'Priority & VLAN' | Set-NetAdapterAdvancedProperty -RegistryValue 0"" >nul 2>&1
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'Wake on Magic Packet' | Set-NetAdapterAdvancedProperty -RegistryValue 0"" >nul 2>&1
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'Wake on magic packet when system is in the S0ix power state' | Set-NetAdapterAdvancedProperty -RegistryValue 0"" >nul 2>&1
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'Wake on pattern match' | Set-NetAdapterAdvancedProperty -RegistryValue 0"" >nul 2>&1
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'WOL & Shutdown Link Speed' | Set-NetAdapterAdvancedProperty -RegistryValue 2"" >nul 2>&1 
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'Jumbo Packet' | Set-NetAdapterAdvancedProperty -RegistryValue 1514"" >nul 2>&1
powershell -Command ""Get-NetAdapterAdvancedProperty -Name \""%adapter_name%\"" -DisplayName 'Jumbo Frame' | Set-NetAdapterAdvancedProperty -RegistryValue 1514"" >nul 2>&1
netsh interface ipv4 add dnsservers name=""%interface%"" address=1.1.1.1 index=1 >nul 2>&1
netsh interface ipv4 add dnsservers name=""%interface%"" address=1.0.0.1 index=2 >nul 2>&1
netsh int tcp set heuristics disabled
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces"" /v ""DisableTaskOffload"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters"" /v ""DisableTaskOffload"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"" /v ""NetworkThrottlingIndex"" /t REG_DWORD /d ""4294967295"" /f
for /f %%q in ('wmic path win32_networkadapter get GUID ^| findstr ""{""') do Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\%%q"" /v TCPNoDelay /t REG_DWORD /d ""1"" /f
for /f %%i in ('wmic path win32_NetworkAdapter get PNPDeviceID') do set ""str=%%i"" & (
Reg.exe add ""HKLM\System\CurrentControlSet\Enum\%%i\Device Parameters\Interrupt Management\Affinity Policy"" /v ""DevicePriority"" /f
Reg.exe add ""HKLM\System\CurrentControlSet\Enum\%%i\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties"" /v ""MSISupported"" /t REG_DWORD /d ""1"" /f
for /f %%q in ('wmic path win32_networkadapter get GUID ^| findstr ""{""') do Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\%%q"" /v InterfaceMetric /t REG_DWORD /d ""55"" /f
for /f %%q in ('wmic path win32_networkadapter get GUID ^| findstr ""{""') do Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\%%q"" /v TcpAckFrequency /t REG_DWORD /d ""1"" /f
for /f %%q in ('wmic path win32_networkadapter get GUID ^| findstr ""{""') do Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\%%q"" /v TcpDelAckTicks /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\ServiceProvider"" /v ""LocalPriority"" /t REG_DWORD /d ""4"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\ServiceProvider"" /v ""HostsPriority"" /t REG_DWORD /d ""5"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\ServiceProvider"" /v ""DnsPriority"" /t REG_DWORD /d ""6"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\ServiceProvider"" /v ""NetbtPriority"" /t REG_DWORD /d ""7"" /f
        ";
            await Task.Run(() => BatchCommands(commands));
        }

        private void deblaot_CheckedChanged(object sender, EventArgs e)
        {
            if (deblaot.Checked) {
                _ = Debloat();
            }
        }

        private async Task Debloat()
        {
            string commands = @"
echo %w% - Disable Customer Experience Improvement Program%b%
schtasks /end /tn ""\Microsoft\Windows\Customer Experience Improvement Program\Consolidator"" > nul 2>&1
schtasks /change /tn ""\Microsoft\Windows\Customer Experience Improvement Program\Consolidator"" /disable > nul 2>&1
schtasks /end /tn ""\Microsoft\Windows\Customer Experience Improvement Program\BthSQM"" > nul 2>&1 
schtasks /change /tn ""\Microsoft\Windows\Customer Experience Improvement Program\BthSQM"" /disable > nul 2>&1
schtasks /end /tn ""\Microsoft\Windows\Customer Experience Improvement Program\KernelCeipTask"" > nul 2>&1
schtasks /change /tn ""\Microsoft\Windows\Customer Experience Improvement Program\KernelCeipTask"" /disable > nul 2>&1
schtasks /end /tn ""\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip"" > nul 2>&1 
schtasks /change /tn ""\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip"" /disable > nul 2>&1
schtasks /end /tn ""\Microsoft\Windows\Customer Experience Improvement Program\Uploader"" > nul 2>&1
schtasks /change /tn ""\Microsoft\Windows\Customer Experience Improvement Program\Uploader"" /disable > nul 2>&1
schtasks /end /tn ""\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser"" > nul 2>&1
schtasks /change /tn ""\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser"" /disable > nul 2>&1
schtasks /end /tn ""\Microsoft\Windows\Application Experience\ProgramDataUpdater"" > nul 2>&1
schtasks /change /tn ""\Microsoft\Windows\Application Experience\ProgramDataUpdater"" /disable > nul 2>&1
schtasks /end /tn ""\Microsoft\Windows\Application Experience\StartupAppTask"" > nul 2>&1
schtasks /end /tn ""\Microsoft\Windows\Shell\FamilySafetyMonitor"" > nul 2>&1
schtasks /change /tn ""\Microsoft\Windows\Shell\FamilySafetyMonitor"" /disable > nul 2>&1
schtasks /end /tn ""\Microsoft\Windows\Shell\FamilySafetyRefresh"" > nul 2>&1
schtasks /change /tn ""\Microsoft\Windows\Shell\FamilySafetyRefresh"" /disable > nul 2>&1
schtasks /end /tn ""\Microsoft\Windows\Shell\FamilySafetyUpload"" > nul 2>&1
schtasks /change /tn ""\Microsoft\Windows\Shell\FamilySafetyUpload"" /disable > nul 2>&1
schtasks /end /tn ""\Microsoft\Windows\Maintenance\WinSAT"" > nul 2>&1
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\Spooler"" /v ""Start"" /t REG_DWORD /d ""4"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\PrintNotify"" /v ""Start"" /t REG_DWORD /d ""4"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\MapsBroker"" /v ""Start"" /t REG_DWORD /d ""4"" /f
Reg.exe add ""HKCU\Software\Microsoft\Office\Common\ClientTelemetry"" /v ""DisableTelemetry"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\Software\Microsoft\Office\16.0\Common"" /v ""sendcustomerdata"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\Office\16.0\Common\Feedback"" /v ""enabled"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\Office\16.0\Common\Feedback"" /v ""includescreenshot"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\Office\16.0\Outlook\Options\Mail"" /v ""EnableLogging"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\Office\16.0\Word\Options"" /v ""EnableLogging"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\Office\Common\ClientTelemetry"" /v ""SendTelemetry"" /t REG_DWORD /d ""3"" /f
Reg.exe add ""HKCU\Software\Microsoft\Office\16.0\Common"" /v ""qmenable"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\Office\16.0\Common"" /v ""updatereliabilitydata"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\Office\16.0\Common\General"" /v ""shownfirstrunoptin"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\Software\Microsoft\Office\16.0\Common\General"" /v ""skydrivesigninoption"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\Office\16.0\Common\ptwatson"" /v ""ptwoptin"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\Office\16.0\Firstrun"" /v ""disablemovie"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\Software\Microsoft\Office\16.0\OSM"" /v ""Enablelogging"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\Office\16.0\OSM"" /v ""EnableUpload"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\Office\16.0\OSM"" /v ""EnableFileObfuscation"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\Software\Microsoft\Office\16.0\OSM\preventedapplications"" /v ""accesssolution"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\Software\Microsoft\Office\16.0\OSM\preventedapplications"" /v ""olksolution"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\Software\Microsoft\Office\16.0\OSM\preventedapplications"" /v ""onenotesolution"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\Software\Microsoft\Office\16.0\OSM\preventedapplications"" /v ""pptsolution"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\Software\Microsoft\Office\16.0\OSM\preventedapplications"" /v ""projectsolution"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\Software\Microsoft\Office\16.0\OSM\preventedapplications"" /v ""publishersolution"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\Software\Microsoft\Office\16.0\OSM\preventedapplications"" /v ""visiosolution"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\Software\Microsoft\Office\16.0\OSM\preventedapplications"" /v ""wdsolution"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\Software\Microsoft\Office\16.0\OSM\preventedapplications"" /v ""xlsolution"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\Software\Microsoft\Office\16.0\OSM\preventedsolutiontypes"" /v ""agave"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\Software\Microsoft\Office\16.0\OSM\preventedsolutiontypes"" /v ""appaddins"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\Software\Microsoft\Office\16.0\OSM\preventedsolutiontypes"" /v ""comaddins"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\Software\Microsoft\Office\16.0\OSM\preventedsolutiontypes"" /v ""documentfiles"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\Software\Microsoft\Office\16.0\OSM\preventedsolutiontypes"" /v ""templatefiles"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\Software\Microsoft\GameBar"" /v ""UseNexusForGameBarEnabled"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\GameBar"" /v ""GameDVR_Enabled"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\Windows\CurrentVersion\GameDVR"" /v ""AppCaptureEnabled"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\Windows\CurrentVersion\GameDVR"" /v ""AudioCaptureEnabled"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\Windows\CurrentVersion\GameDVR"" /v ""CursorCaptureEnabled"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\Windows\CurrentVersion\GameDVR"" /v ""HistoricalCaptureEnabled"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\System\GameConfigStore"" /v ""GameDVR_Enabled"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\Software\Policies\Microsoft\Windows\GameDVR"" /v ""AllowgameDVR"" /t REG_DWORD /d ""0"" /f
sc config xbgm start= disabled >nul 2>&1
sc config XblAuthManager start= disabled
sc config XblGameSave start= disabled
sc config XboxGipSvc start= disabled
sc config XboxNetApiSvc start= disabled
echo Disabling Start Up Apps 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"" /v ""Discord"" /t REG_BINARY /d ""0300000066AF9C7C5A46D901"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"" /v ""Synapse3"" /t REG_BINARY /d ""030000007DC437B0EA9FD901"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"" /v ""Spotify"" /t REG_BINARY /d ""0300000070E93D7B5A46D901"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"" /v ""EpicGamesLauncher"" /t REG_BINARY /d ""03000000F51C70A77A48D901"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"" /v ""RiotClient"" /t REG_BINARY /d ""03000000A0EA598A88B2D901"" /f 
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"" /v ""Steam"" /t REG_BINARY /d ""03000000E7766B83316FD901"" /f
echo UNINSTALLING PREINSTALLED APPS FROM YOUR PC  ITS GONNA TAKE FEW MINUTES 
Powershell.exe -command ""& {Get-AppxPackage *Microsoft.BingWeather* | Remove-AppxPackage}
echo %w%- Uninstalling BingWeather %b%
Powershell.exe -command ""& {Get-AppxPackage *Microsoft.GetHelp* | Remove-AppxPackage}
echo %w%- Uninstalling GetHelp %b%
Powershell.exe -command ""& {Get-AppxPackage *Microsoft.Getstarted* | Remove-AppxPackage}
echo %w%- Uninstalling Getstarted %b%
Powershell.exe -command ""& {Get-AppxPackage *Microsoft.Messaging* | Remove-AppxPackage}
echo %w%- Uninstalling Messaging %b%
Powershell.exe -command ""& {Get-AppxPackage *Microsoft.Microsoft3DViewer* | Remove-AppxPackage}
echo %w%- Uninstalling Microsoft3DViewer %b%
Powershell.exe -command ""& {Get-AppxPackage *Microsoft.MicrosoftSolitaireCollection* | Remove-AppxPackage}
echo %w%- Uninstalling MicrosoftSolitaireCollection %b%
Powershell.exe -command ""& {Get-AppxPackage *Microsoft.MicrosoftStickyNotes* | Remove-AppxPackage}
echo %w%- Uninstalling MicrosoftStickyNotes %b%
Powershell.exe -command ""& {Get-AppxPackage *Microsoft.MixedReality.Portal* | Remove-AppxPackage}
echo %w%- Uninstalling MixedReality.Portal %b%
Powershell.exe -command ""& {Get-AppxPackage *Microsoft.OneConnect* | Remove-AppxPackage}
echo %w%- Uninstalling OneConnect %b%
Powershell.exe -command ""& {Get-AppxPackage *Microsoft.People* | Remove-AppxPackage}
echo %w%- Uninstalling People %b%
Powershell.exe -command ""& {Get-AppxPackage *Microsoft.Print3D* | Remove-AppxPackage}
echo %w%- Uninstalling Print3D %b%
Powershell.exe -command ""& {Get-AppxPackage *Microsoft.SkypeApp* | Remove-AppxPackage}
echo %w%- Uninstalling SkypeApp %b%
Powershell.exe -command ""& {Get-AppxPackage *Microsoft.WindowsAlarms* | Remove-AppxPackage}
echo %w%- Uninstalling WindowsAlarms %b%
Powershell.exe -command ""& {Get-AppxPackage *Microsoft.WindowsCamera* | Remove-AppxPackage}
echo %w%- Uninstalling WindowsCamera %b%
Powershell.exe -command ""& {Get-AppxPackage *microsoft.windowscommunicationsapps* | Remove-AppxPackage}
echo %w%- Uninstalling windowscommunicationsapps %b%
Powershell.exe -command ""& {Get-AppxPackage *Microsoft.WindowsMaps* | Remove-AppxPackage}
echo %w%- Uninstalling WindowsMaps %b%
Powershell.exe -command ""& {Get-AppxPackage *Microsoft.WindowsFeedbackHub* | Remove-AppxPackage}
echo %w%- Uninstalling WindowsFeedbackHub %b%
Powershell.exe -command ""& {Get-AppxPackage *Microsoft.WindowsSoundRecorder* | Remove-AppxPackage}
echo %w%- Uninstalling WindowsSoundRecorder %b%
Powershell.exe -command ""& {Get-AppxPackage *Microsoft.YourPhone* | Remove-AppxPackage}
echo %w%- Uninstalling YourPhone %b%
Powershell.exe -command ""& {Get-AppxPackage *Microsoft.ZuneMusic* | Remove-AppxPackage}
echo %w%- Uninstalling ZuneMusic %b%
Powershell.exe -command ""& {Get-AppxPackage *Microsoft.HEIFImageExtension* | Remove-AppxPackage}
echo %w%- Uninstalling HEIFImageExtension %b%
Powershell.exe -command ""& {Get-AppxPackage *Microsoft.WebMediaExtensions* | Remove-AppxPackage}
echo %w%- Uninstalling WebMediaExtensions %b%
Powershell.exe -command ""& {Get-AppxPackage *Microsoft.WebpImageExtension* | Remove-AppxPackage}
echo %w%- Uninstalling WebpImageExtension %b%
Powershell.exe -command ""& {Get-AppxPackage *Microsoft.3dBuilder* | Remove-AppxPackage}
echo %w%- Uninstalling 3dBuilder %b%
PowerShell -Command ""Get-AppxPackage -allusers *bing* | Remove-AppxPackage""
echo %w%- Uninstalling bing %b%
PowerShell -Command ""Get-AppxPackage -allusers *bingfinance* | Remove-AppxPackage""
echo %w%- Uninstalling bingfinance %b%
PowerShell -Command ""Get-AppxPackage -allusers *bingsports* | Remove-AppxPackage""
echo %w%- Uninstalling bingsports %b%
PowerShell -Command ""Get-AppxPackage -allusers *CommsPhone* | Remove-AppxPackage""
echo %w%- Uninstalling CommsPhone %b%
PowerShell -Command ""Get-AppxPackage -allusers *Drawboard PDF* | Remove-AppxPackage""
echo %w%- Uninstalling Drawboard PDF %b%
echo %w%- Uninstalling Sway %b%
PowerShell -Command ""Get-AppxPackage -allusers *Sway* | Remove-AppxPackage""
echo %w%- Uninstalling WindowsAlarms %b%
PowerShell -Command ""Get-AppxPackage -allusers *WindowsAlarms* | Remove-AppxPackage""
echo %w%- Uninstalling WindowsPhone %b%
PowerShell -Command ""Get-AppxPackage -allusers *WindowsPhone* | Remove-AppxPackage""
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v ""AllowCortana"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v ""AllowCloudSearch"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v ""AllowCortanaAboveLock"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v ""AllowSearchToUseLocation"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v ""ConnectedSearchUseWeb"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v ""ConnectedSearchUseWebOverMeteredConnections"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v ""DisableWebSearch"" /t REG_DWORD /d ""0"" /f
        ";
            await Task.Run(() => BatchCommands(commands));
        }

        private void cpu_CheckedChanged(object sender, EventArgs e)
        {
            if (cpu.Checked) {
                _ = CpuTweaks();
            }
        }

        private async Task CpuTweaks()
        {
            string commands = @"
bcdedit /set {current} numproc %NUMBER_OF_PROCESSORS%
powercfg /setACvalueindex scheme_current SUB_PROCESSOR SYSCOOLPOL 1
powercfg /setDCvalueindex scheme_current SUB_PROCESSOR SYSCOOLPOL 1
powercfg /setactive SCHEME_CURRENT
Powercfg -setdcvalueindex scheme_current sub_processor PROCTHROTTLEMAX 100
Powercfg -setactive scheme_current
Powercfg -setdcvalueindex scheme_current sub_processor PROCTHROTTLEMIN 50
Powercfg -setactive scheme_current
Powercfg -setacvalueindex scheme_current sub_processor PROCTHROTTLEMAX 100
Powercfg -setactive scheme_current
Powercfg -setacvalueindex scheme_current sub_processor PROCTHROTTLEMIN 100
Powercfg -setactive scheme_current
powercfg -setacvalueindex scheme_current sub_processor CPMINCORES 100
powercfg /setactive SCHEME_CURRENT
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\IntelPPM"" /v Start /t REG_DWORD /d 3 /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\AmdPPM"" /v Start /t REG_DWORD /d 3 /
        ";
            await Task.Run(() => BatchCommands(commands));
        }

        private void services_CheckedChanged(object sender, EventArgs e)
        {
            if (services.Checked) {
                _ = Services();
            }
        }

        private async Task Services()
        {
            string commands = @"
schtasks /Change /TN ""Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Application Experience\PcaPatchDbTask"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Application Experience\ProgramDataUpdater"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Application Experience\StartupAppTask"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Autochk\Proxy"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Customer Experience Improvement Program\Consolidator"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Customer Experience Improvement Program\UsbCeip"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Defrag\ScheduledDefrag"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Device Information\Device"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Device Information\Device User"" /Disable
schtasks /Change /TN ""Microsoft\Windows\DiskCleanup\SilentCleanup"" /Disable
schtasks /Change /TN ""Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector"" /Disable
schtasks /Change /TN ""Microsoft\Windows\DiskFootprint\Diagnostics"" /Disable
schtasks /Change /TN ""Microsoft\Windows\DiskFootprint\StorageSense"" /Disable
schtasks /Change /TN ""Microsoft\Windows\DUSM\dusmtask"" /Disable
schtasks /Change /TN ""Microsoft\Windows\EnterpriseMgmt\MDMMaintenenceTask"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Feedback\Siuf\DmClient"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Feedback\Siuf\DmClientOnScenarioDownload"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Flighting\FeatureConfig\ReconcileFeatures"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Flighting\FeatureConfig\UsageDataFlushing"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Flighting\FeatureConfig\UsageDataReporting"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Flighting\OneSettings\RefreshCache"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Input\LocalUserSyncDataAvailable"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Input\MouseSyncDataAvailable"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Input\PenSyncDataAvailable"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Input\TouchpadSyncDataAvailable"" /Disable
schtasks /Change /TN ""Microsoft\Windows\International\Synchronize Language Settings"" /Disable
schtasks /Change /TN ""Microsoft\Windows\LanguageComponentsInstaller\Installation"" /Disable
schtasks /Change /TN ""Microsoft\Windows\LanguageComponentsInstaller\ReconcileLanguageResources"" /Disable
schtasks /Change /TN ""Microsoft\Windows\LanguageComponentsInstaller\Uninstallation"" /Disable
schtasks /Change /TN ""Microsoft\Windows\License Manager\TempSignedLicenseExchange"" /Disable
schtasks /Change /TN ""Microsoft\Windows\License Manager\TempSignedLicenseExchange"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Management\Provisioning\Cellular"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Management\Provisioning\Logon"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Maintenance\WinSAT"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Maps\MapsToastTask"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Maps\MapsUpdateTask"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Mobile Broadband Accounts\MNO Metadata Parser"" /Disable
schtasks /Change /TN ""Microsoft\Windows\MUI\LPRemove"" /Disable
schtasks /Change /TN ""Microsoft\Windows\NetTrace\GatherNetworkInfo"" /Disable
schtasks /Change /TN ""Microsoft\Windows\PI\Sqm-Tasks"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Power Efficiency Diagnostics\AnalyzeSystem"" /Disable
schtasks /Change /TN ""Microsoft\Windows\PushToInstall\Registration"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Ras\MobilityManager"" /Disable
schtasks /Change /TN ""Microsoft\Windows\RecoveryEnvironment\VerifyWinRE"" /Disable
schtasks /Change /TN ""Microsoft\Windows\RemoteAssistance\RemoteAssistanceTask"" /Disable
schtasks /Change /TN ""Microsoft\Windows\RetailDemo\CleanupOfflineContent"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Servicing\StartComponentCleanup"" /Disable
schtasks /Change /TN ""Microsoft\Windows\SettingSync\NetworkStateChangeTask"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Setup\SetupCleanupTask"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Setup\SnapshotCleanupTask"" /Disable
schtasks /Change /TN ""Microsoft\Windows\SpacePort\SpaceAgentTask"" /Disable
schtasks /Change /TN ""Microsoft\Windows\SpacePort\SpaceManagerTask"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Speech\SpeechModelDownloadTask"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Storage Tiers Management\Storage Tiers Management Initialization"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Sysmain\ResPriStaticDbSync"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Sysmain\WsSwapAssessmentTask"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Task Manager\Interactive"" /Disable
schtasks /Change /TN ""Microsoft\Windows\TPM\Tpm-HASCertRetr"" /Disable
schtasks /Change /TN ""Microsoft\Windows\TPM\Tpm-Maintenance"" /Disable
schtasks /Change /TN ""Microsoft\Windows\UPnP\UPnPHostConfig"" /Disable
schtasks /Change /TN ""Microsoft\Windows\User Profile Service\HiveUploadTask"" /Disable
schtasks /Change /TN ""Microsoft\Windows\WDI\ResolutionHost"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Windows Filtering Platform\BfeOnServiceStartTypeChange"" /Disable
schtasks /Change /TN ""Microsoft\Windows\WOF\WIM-Hash-Management"" /Disable
schtasks /Change /TN ""Microsoft\Windows\WOF\WIM-Hash-Validation"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Work Folders\Work Folders Logon Synchronization"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Work Folders\Work Folders Maintenance Work"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Workplace Join\Automatic-Device-Join"" /Disable
schtasks /Change /TN ""Microsoft\Windows\WwanSvc\NotificationTask"" /Disable
schtasks /Change /TN ""Microsoft\Windows\WwanSvc\OobeDiscovery"" /Disable
        ";
            await Task.Run(() => BatchCommands(commands));
        }

        private void performance_CheckedChanged(object sender, EventArgs e)
        {
            if (performance.Checked) {
                MessageBox.Show("Sve ugasite sem Show thumbnails instead of icon i smooth edges on screen fonts i pritisnite apply");
                Process.Start("SystemPropertiesPerformance.exe");
            }
        }

        private void gpu_CheckedChanged(object sender, EventArgs e)
        {
            if (gpu.Checked) {
                _ = Gpu();
            }
        }

        private async Task Gpu()
        {
            string commands = @"
for /f %%i in ('reg query ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /s /f Scaling') do set ""str=%%i"" & if ""!str!"" neq ""!str:Configuration\=!"" (
	Reg.exe add ""%%i"" /v ""Scaling"" /t REG_DWORD /d ""1"" /f 
)
echo MMCSS Gpu tweaks
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\DisplayPostProcessing"" /v ""Affinity"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\DisplayPostProcessing"" /v ""Background Only"" /t REG_SZ /d ""True"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\DisplayPostProcessing"" /v ""BackgroundPriority"" /t REG_DWORD /d ""24"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\DisplayPostProcessing"" /v ""Clock Rate"" /t REG_DWORD /d ""10000"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\DisplayPostProcessing"" /v ""GPU Priority"" /t REG_DWORD /d ""18"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\DisplayPostProcessing"" /v ""Priority"" /t REG_DWORD /d ""8"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\DisplayPostProcessing"" /v ""Scheduling Category"" /t REG_SZ /d ""High"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\DisplayPostProcessing"" /v ""SFIO Priority"" /t REG_SZ /d ""High"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\DisplayPostProcessing"" /v ""Latency Sensitive"" /t REG_SZ /d ""True"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v ""TdrLevel"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v ""TdrDebugMode"" /t REG_DWORD /d ""0"" /f
timeout /t 1 /nobreak > NUL
echo Graphics card shedueler tweaks
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Scheduler"" /v ""VsyncIdleTimeout"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v ""TdrDebugMode"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v ""TdrLevel"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\System\CurrentControlSet\Services\VxD\BIOS"" /v ""AGPConcur"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\System\CurrentControlSet\Services\VxD\BIOS"" /v ""CPUPriority"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\System\CurrentControlSet\Services\VxD\BIOS"" /v ""FastDRAM"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\System\CurrentControlSet\Services\VxD\BIOS"" /v ""PCIConcur"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\System\CurrentControlSet\Control\GraphicsDrivers"" /v TdrLevel /t REG_DWORD /d 0 /f
Reg.exe add ""HKLM\System\CurrentControlSet\Control\GraphicsDrivers"" /v TdrDelay /t REG_DWORD /d 60 /f
timeout /t 1 /nobreak > NUL
echo Disable Preemption
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Scheduler"" /v ""EnablePreemption"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Scheduler"" /v ""GPUPreemptionLevel"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Scheduler"" /v ""EnableAsyncMidBufferPreemption"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Scheduler"" /v ""EnableMidGfxPreemptionVGPU"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Scheduler"" /v ""EnableMidBufferPreemptionForHighTdrTimeout"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Scheduler"" /v ""EnableSCGMidBufferPreemption"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Scheduler"" /v ""PerfAnalyzeMidBufferPreemption"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Scheduler"" /v ""EnableMidGfxPreemption"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Scheduler"" /v ""EnableMidBufferPreemption"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Scheduler"" /v ""EnableCEPreemption"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Scheduler"" /v ""DisableCudaContextPreemption"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Scheduler"" /v ""DisablePreemptionOnS3S4"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Scheduler"" /v ""ComputePreemptionLevel"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Scheduler"" /v ""DisablePreemption"" /t REG_DWORD /d ""1"" /f
timeout /t 1 /nobreak > NUL
echo MSI Mode
for /f %%n in ('wmic path win32_videocontroller get PNPDeviceID ^| findstr /L ""VEN_""') do (
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Enum\%%n\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties"" /v ""MSISupported"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Enum\%%n\Device Parameters\Interrupt Management\Affinity Policy"" /v ""DevicePriority"" /t REG_DWORD /d ""0"" /f
)
timeout /t 1 /nobreak > NUL
echo Disable GpuEnergyDrv
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\GpuEnergyDrv"" /v ""Start"" /t REG_DWORD /d ""4"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\GpuEnergyDr"" /v ""Start"" /t REG_DWORD /d ""4"" /f
echo Latency Tolernace
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""ExitLatency"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""ExitLatencyCheckEnabled"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""Latency"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""LatencyToleranceDefault"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""LatencyToleranceFSVP"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""LatencyTolerancePerfOverride"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""LatencyToleranceScreenOffIR"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""LatencyToleranceVSyncEnabled"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""RtlCapabilityCheckLatency"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""QosManagesIdleProcessors"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DisableVsyncLatencyUpdate"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DisableSensorWatchdog"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""CoalescingTimerInterval"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""InterruptSteeringDisabled"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""LowLatencyScalingPercentage"" /t REG_DWORD /d ""100"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""HighPerformance"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""HighestPerformance"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""MinimumThrottlePercent"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""MaximumThrottlePercent"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""MaximumPerformancePercent"" /t REG_DWORD /d ""100"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""InitialUnparkCount"" /t REG_DWORD /d ""100"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultD3TransitionLatencyActivelyUsed"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultD3TransitionLatencyIdleLongTime"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultD3TransitionLatencyIdleMonitorOff"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultD3TransitionLatencyIdleNoContext"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultD3TransitionLatencyIdleShortTime"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultD3TransitionLatencyIdleVeryLongTime"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultLatencyToleranceIdle0"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultLatencyToleranceIdle0MonitorOff"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultLatencyToleranceIdle1"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultLatencyToleranceIdle1MonitorOff"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultLatencyToleranceMemory"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultLatencyToleranceNoContext"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultLatencyToleranceNoContextMonitorOff"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultLatencyToleranceOther"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultLatencyToleranceTimerPeriod"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultMemoryRefreshLatencyToleranceActivelyUsed"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultMemoryRefreshLatencyToleranceMonitorOff"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultMemoryRefreshLatencyToleranceNoContext"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""Latency"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""MiracastPerfTrackGraphicsLatency"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""MonitorLatencyTolerance"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""MonitorRefreshLatencyTolerance"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""TransitionLatency"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DisableVsyncLatencyUpdate"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DisableSensorWatchdog"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""InterruptSteeringDisabled"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""ExitLatency"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""ExitLatencyCheckEnabled"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""Latency"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""LatencyToleranceDefault"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""LatencyToleranceFSVP"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""LatencyTolerancePerfOverride"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""LatencyToleranceScreenOffIR"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""LatencyToleranceVSyncEnabled"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""RtlCapabilityCheckLatency"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""LowLatencyScalingPercentage"" /t REG_DWORD /d ""100"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""MaxIAverageGraphicsLatencyInOneBucket"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultD3TransitionLatencyActivelyUsed"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultD3TransitionLatencyIdleLongTime"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultD3TransitionLatencyIdleMonitorOff"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultD3TransitionLatencyIdleNoContext"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultD3TransitionLatencyIdleShortTime"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultD3TransitionLatencyIdleVeryLongTime"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultLatencyToleranceIdle0"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultLatencyToleranceIdle0MonitorOff"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultLatencyToleranceIdle1"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultLatencyToleranceIdle1MonitorOff"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultLatencyToleranceMemory"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultLatencyToleranceNoContext"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultLatencyToleranceNoContextMonitorOff"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultLatencyToleranceOther"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultLatencyToleranceTimerPeriod"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultMemoryRefreshLatencyToleranceActivelyUsed"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultMemoryRefreshLatencyToleranceMonitorOff"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""DefaultMemoryRefreshLatencyToleranceNoContext"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""MaxIAverageGraphicsLatencyInOneBucket"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""Latency"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""MiracastPerfTrackGraphicsLatency"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""MonitorLatencyTolerance"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""MonitorRefreshLatencyTolerance"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""TransitionLatency"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v ""MonitorLatencyTolerance"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v ""MonitorRefreshLatencyTolerance"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Power"" /v ""MonitorLatencyTolerance"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Power"" /v ""MonitorRefreshLatencyTolerance"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v ""RMDisablePostL2Compression"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v ""RmDisableRegistryCaching"" /t REG_DWORD /d ""1"" /f
        ";
            await Task.Run(() => BatchCommands(commands));
        }

        private void advanced_CheckedChanged(object sender, EventArgs e)
        {
            if (advanced.Checked) {
                _ = Advanced();
            }
        }

        private async Task Advanced()
        {

            string commands = @"
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Games"" /v ""FpsAll"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Games"" /v ""FpsStatusGames"" /t REG_DWORD /d ""10"" /f
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Games"" /v ""FpsStatusGamesAll"" /t REG_DWORD /d ""4"" /f
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Games"" /v ""GameFluidity"" /t REG_DWORD /d ""1"" /f
sc config xbgm start= disabled
sc config XblAuthManager start= disabled
sc config XblGameSave start= disabled
sc config XboxGipSvc start= disabled
sc config XboxNetApiSvc start= disabled
Reg.exe add ""HKCU\Software\Microsoft\GameBar"" /v ""AllowAutoGameMode"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\GameBar"" /v ""ShowStartupPanel"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\GameBar"" /v ""UseNexusForGameBarEnabled"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\GameBar"" /v ""GameDVR_Enabled"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\Windows\CurrentVersion\GameDVR"" /v ""AppCaptureEnabled"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\Windows\CurrentVersion\GameDVR"" /v ""AudioCaptureEnabled"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\Windows\CurrentVersion\GameDVR"" /v ""CursorCaptureEnabled"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\Windows\CurrentVersion\GameDVR"" /v ""HistoricalCaptureEnabled"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\System\GameConfigStore"" /v ""GameDVR_Enabled"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\Software\Policies\Microsoft\Windows\GameDVR"" /v ""AllowgameDVR"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\xbgm"" /v Start /t REG_DWORD /d 0 /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\XboxGipSvc"" /v Start /t REG_DWORD /d 0 /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\XblAuthManager"" /v Start /t REG_DWORD /d 0 /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\XblGameSave"" /v Start /t REG_DWORD /d 0 /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\XboxNetApiSvc"" /v Start /t REG_DWORD /d 0 /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v ""D3PCLatency"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v ""F1TransitionLatency"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v ""LOWLATENCY"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v ""Node3DLowLatency"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v ""PciLatencyTimerControl"" /t REG_DWORD /d ""20"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v ""RMDeepL1EntryLatencyUsec"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v ""RmGspcMaxFtuS"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v ""RmGspcMinFtuS"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v ""RmGspcPerioduS"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v ""RMLpwrEiIdleThresholdUs"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v ""RMLpwrGrIdleThresholdUs"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v ""RMLpwrGrRgIdleThresholdUs"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v ""RMLpwrMsIdleThresholdUs"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v ""VRDirectFlipDPCDelayUs"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v ""VRDirectFlipTimingMarginUs"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v ""VRDirectJITFlipMsHybridFlipDelayUs"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v ""vrrCursorMarginUs"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v ""vrrDeflickerMarginUs"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v ""vrrDeflickerMaxUs"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Search"" /v ""BingSearchEnabled"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\InputPersonalization"" /v ""RestrictImplicitInkCollection"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\Software\Microsoft\InputPersonalization"" /v ""RestrictImplicitTextCollection"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\Software\Microsoft\Personalization\Settings"" /v ""AcceptedPrivacyPolicy"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Search"" /v ""CortanaCapabilities"" /t REG_SZ /d """" /f
Reg.exe add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Search"" /v ""IsAssignedAccess"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Search"" /v ""IsWindowsHelloActive"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v ""AllowIndexingEncryptedStoresOrItems"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v ""AllowSearchToUseLocation"" /t REG_DWORD /d 0 /f
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v ""ConnectedSearchPrivacy"" /t REG_DWORD /d 3 /f
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v ""ConnectedSearchSafeSearch"" /t REG_DWORD /d 3 /f
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v ""ConnectedSearchUseWeb"" /t REG_DWORD /d 0 /f
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v ""ConnectedSearchUseWebOverMeteredConnections"" /t REG_DWORD /d 0 /f
Reg.exe add ""HKLM\Software\Microsoft\PolicyManager\default\Experience\AllowCortana"" /v ""value"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\Software\Policies\Microsoft\SearchCompanion"" /v ""DisableContentFileUpdates"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\Software\Policies\Microsoft\Windows\Windows Search"" /v ""AllowCloudSearch"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\Software\Policies\Microsoft\Windows\Windows Search"" /v ""AllowCortanaAboveLock"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\Software\Policies\Microsoft\Windows\Windows Search"" /v ""AllowSearchToUseLocation"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\Software\Policies\Microsoft\Windows\Windows Search"" /v ""ConnectedSearchPrivacy"" /t REG_DWORD /d ""3"" /f
Reg.exe add ""HKLM\Software\Policies\Microsoft\Windows\Windows Search"" /v ""ConnectedSearchUseWeb"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\Software\Policies\Microsoft\Windows\Windows Search"" /v ""ConnectedSearchUseWebOverMeteredConnections"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\Software\Policies\Microsoft\Windows\Windows Search"" /v ""DisableWebSearch"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\Software\Policies\Microsoft\Windows\Windows Search"" /v ""DoNotUseWebResults"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"" /v ""NoLazyMode"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"" /v ""AlwaysOn"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\AppModel"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\Cellcore"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\Circular Kernel Context Logger"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\CloudExperienceHostOobe"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\DataMarket"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\DefenderApiLogger"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\DefenderAuditLogger"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\DiagLog"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\HolographicDevice"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\iclsClient"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\iclsProxy"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\LwtNetLog"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\Mellanox-Kernel"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\Microsoft-Windows-AssignedAccess-Trace"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\Microsoft-Windows-Setup"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\NBSMBLOGGER"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\PEAuthLog"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\RdrLog"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\ReadyBoot"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\SetupPlatform"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\SetupPlatformTel"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\SocketHeciServer"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\SpoolerLogger"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\SQMLogger"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\TCPIPLOGGER"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\TileStore"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\Tpm"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\TPMProvisioningService"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\UBPM"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\WdiContextLog"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\WFP-IPsec Trace"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\WiFiDriverIHVSession"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\WiFiDriverIHVSessionRepro"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\WiFiSession"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\WinPhoneCritical"" /v ""Start"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\WUDF"" /v ""LogEnable"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\WUDF"" /v ""LogLevel"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent"" /v ""DisableThirdPartySuggestions"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent"" /v ""DisableWindowsConsumerFeatures"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Lsa\Credssp"" /v ""DebugLogLevel"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\DXGKrnl"" /v ""MonitorLatencyTolerance"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\DXGKrnl"" /v ""MonitorRefreshLatencyTolerance"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\DXGKrnl"" /v ""MonitorLatencyTolerance"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\DXGKrnl"" /v ""MonitorRefreshLatencyTolerance"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel"" /v ""DpcWatchdogProfileOffset"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel"" /v ""DisableExceptionChainValidation"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel"" /v ""KernelSEHOPEnabled"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel"" /v ""DisableAutoBoost"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel"" /v ""DpcTimeout"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel"" /v ""ThreadDpcEnable"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel"" /v ""DpcWatchdogPeriod"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel"" /v ""InterruptSteeringDisabled"" /t REG_DWORD /d ""1"" /f
        ";
            await Task.Run(() => BatchCommands(commands));
        }

        private void gameopti_CheckedChanged(object sender, EventArgs e)
        {
            if (gameopti.Checked) {
                _ = Gameopti();
            }
        }

        private async Task Gameopti()
        {
            string commands = @"
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Search"" /v ""BingSearchEnabled"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\InputPersonalization"" /v ""RestrictImplicitInkCollection"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\Software\Microsoft\InputPersonalization"" /v ""RestrictImplicitTextCollection"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\Software\Microsoft\Personalization\Settings"" /v ""AcceptedPrivacyPolicy"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Search"" /v ""CortanaCapabilities"" /t REG_SZ /d """" /f
Reg.exe add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Search"" /v ""IsAssignedAccess"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Search"" /v ""IsWindowsHelloActive"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v ""AllowSearchToUseLocation"" /t REG_DWORD /d 0 /f
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v ""ConnectedSearchPrivacy"" /t REG_DWORD /d 3 /f
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v ""ConnectedSearchSafeSearch"" /t REG_DWORD /d 3 /f
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v ""ConnectedSearchUseWeb"" /t REG_DWORD /d 0 /f
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v ""ConnectedSearchUseWebOverMeteredConnections"" /t REG_DWORD /d 0 /f
Reg.exe add ""HKLM\Software\Microsoft\PolicyManager\default\Experience\AllowCortana"" /v ""value"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\Software\Policies\Microsoft\SearchCompanion"" /v ""DisableContentFileUpdates"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\Software\Policies\Microsoft\Windows\Windows Search"" /v ""AllowCloudSearch"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\Software\Policies\Microsoft\Windows\Windows Search"" /v ""AllowCortanaAboveLock"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\Software\Policies\Microsoft\Windows\Windows Search"" /v ""AllowSearchToUseLocation"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\Software\Policies\Microsoft\Windows\Windows Search"" /v ""ConnectedSearchPrivacy"" /t REG_DWORD /d ""3"" /f
Reg.exe add ""HKLM\Software\Policies\Microsoft\Windows\Windows Search"" /v ""ConnectedSearchUseWeb"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\Software\Policies\Microsoft\Windows\Windows Search"" /v ""ConnectedSearchUseWebOverMeteredConnections"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\Software\Policies\Microsoft\Windows\Windows Search"" /v ""DisableWebSearch"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKLM\Software\Policies\Microsoft\Windows\Windows Search"" /v ""DoNotUseWebResults"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Games"" /v ""FpsAll"" /t REG_DWORD /d ""1"" /f
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Games"" /v ""FpsStatusGames"" /t REG_DWORD /d ""10"" /f
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Games"" /v ""FpsStatusGamesAll"" /t REG_DWORD /d ""4"" /f
Reg.exe add ""HKCU\SOFTWARE\Microsoft\Games"" /v ""GameFluidity"" /t REG_DWORD /d ""1"" /f
for /f %%q in ('wmic path win32_networkadapter get GUID ^| findstr ""{""') do Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\%%q"" /v InterfaceMetric /t REG_DWORD /d 0000055 /f
for /f %%q in ('wmic path win32_networkadapter get GUID ^| findstr ""{""') do Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\%%q"" /v TCPNoDelay /t REG_DWORD /d 0000001 /f
for /f %%q in ('wmic path win32_networkadapter get GUID ^| findstr ""{""') do Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\%%q"" /v TcpAckFrequency /t REG_DWORD /d 0000001 /f
for /f %%q in ('wmic path win32_networkadapter get GUID ^| findstr ""{""') do Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\%%q"" /v TcpDelAckTicks /t REG_DWORD /d 0000000 /f
powercfg /h off
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""HibernateEnabled"" /t REG_DWORD /d ""0"" /f 
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\Power"" /v ""SleepReliabilityDetailedDiagnostics"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"" /v ""Affinity"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"" /v ""Background Only"" /t REG_SZ /d ""False"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"" /v ""BackgroundPriority"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"" /v ""Clock Rate"" /t REG_DWORD /d ""10000"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"" /v ""GPU Priority"" /t REG_DWORD /d ""8"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"" /v ""Priority"" /t REG_DWORD /d ""2"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"" /v ""Scheduling Category"" /t REG_SZ /d ""High"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"" /v ""SFIO Priority"" /t REG_SZ /d ""High"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"" /v ""Latency Sensitive"" /t REG_SZ /d ""True"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Low Latency"" /v ""Affinity"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Low Latency"" /v ""Background Only"" /t REG_SZ /d ""False"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Low Latency"" /v ""BackgroundPriority"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Low Latency"" /v ""Clock Rate"" /t REG_DWORD /d ""10000"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Low Latency"" /v ""GPU Priority"" /t REG_DWORD /d ""8"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Low Latency"" /v ""Priority"" /t REG_DWORD /d ""2"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Low Latency"" /v ""Scheduling Category"" /t REG_SZ /d ""Medium"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Low Latency"" /v ""SFIO Priority"" /t REG_SZ /d ""High"" /f
Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Low Latency"" /v ""Latency Sensitive"" /t REG_SZ /d ""True"" /f
Reg.exe add ""HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl"" /v ""Win32PrioritySeparation"" /t REG_DWORD /d ""38"" /f
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\DeviceGuard"" /v ""EnableVirtualizationBasedSecurity"" /t REG_DWORD /d ""0"" /f
Reg.exe add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\DeviceGuard"" /v ""HVCIMATRequired"" /t REG_DWORD /d ""0"" /f
        ";
            await Task.Run(() => BatchCommands(commands));
        }

        private void ctt_CheckedChanged(object sender, EventArgs e)
        {
            if (ctt.Checked) {
                _ = CTT();
            }
        }

        private async Task CTT()
        {
            string commands = @"
reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\System"" /v ""EnableActivityFeed"" /t REG_DWORD /d 0 /f
reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\System"" /v ""PublishUserActivities"" /t REG_DWORD /d 0 /f
reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\System"" /v ""UploadUserActivities"" /t REG_DWORD /d 0 /f
reg add ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location"" /v ""Value"" /t REG_SZ /d ""Deny"" /f
reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Sensor\Overrides\{BFA794E4-F964-4FDB-90F6-51056BFE4B44}"" /v ""SensorPermissionState"" /t REG_DWORD /d 0 /f
reg add ""HKLM\SYSTEM\CurrentControlSet\Services\lfsvc\Service\Configuration"" /v ""Status"" /t REG_DWORD /d 0 /f
reg add ""HKLM\SYSTEM\Maps"" /v ""AutoUpdateEnabled"" /t REG_DWORD /d 0 /f
reg add ""HKCU\Software\Policies\Microsoft\Windows\Explorer"" /v DisableNotificationCenter /t REG_DWORD /d 1 /f
reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\PushNotifications"" /v ToastEnabled /t REG_DWORD /d 0 /f
reg add ""HKEY_CURRENT_USER\Control Panel\Accessibility\StickyKeys"" /v Flags /t REG_SZ /d 506 /f
reg add ""HKU\.DEFAULT\Control Panel\Keyboard"" /v InitialKeyboardIndicators /t REG_DWORD /d 80000002 /f
reg add ""HKCU\Control Panel\Desktop"" /v ""DragFullWindows"" /t REG_SZ /d ""0"" /f
reg add ""HKCU\Control Panel\Desktop"" /v ""MenuShowDelay"" /t REG_SZ /d ""200"" /f
reg add ""HKCU\Control Panel\Desktop\WindowMetrics"" /v ""MinAnimate"" /t REG_SZ /d ""0"" /f
reg add ""HKCU\Control Panel\Keyboard"" /v ""KeyboardDelay"" /t REG_DWORD /d 0 /f
reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v ""ListviewAlphaSelect"" /t REG_DWORD /d 0 /f
reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v ""ListviewShadow"" /t REG_DWORD /d 0 /f
reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v ""TaskbarAnimations"" /t REG_DWORD /d 0 /f
reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects"" /v ""VisualFXSetting"" /t REG_DWORD /d 3 /f
reg add ""HKCU\Software\Microsoft\Windows\DWM"" /v ""EnableAeroPeek"" /t REG_DWORD /d 0 /f
reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v ""TaskbarMn"" /t REG_DWORD /d 0 /f
reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v ""TaskbarDa"" /t REG_DWORD /d 0 /f
reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v ""ShowTaskViewButton"" /t REG_DWORD /d 0 /f
reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Search"" /v ""SearchboxTaskbarMode"" /t REG_DWORD /d 0 /f
powershell -Command ""Set-ItemProperty -Path 'HKCU:\Control Panel\Desktop' -Name 'UserPreferencesMask' -Type Binary -Value ([byte[]](144,18,3,128,16,0,0,0))""
reg add ""HKCU\System\GameConfigStore"" /v GameDVR_FSEBehavior /t REG_DWORD /d 2 /f
reg add ""HKCU\System\GameConfigStore"" /v GameDVR_Enabled /t REG_DWORD /d 0 /f
reg add ""HKCU\System\GameConfigStore"" /v GameDVR_DXGIHonorFSEWindowsCompatible /t REG_DWORD /d 1 /f
reg add ""HKCU\System\GameConfigStore"" /v GameDVR_HonorUserFSEBehaviorMode /t REG_DWORD /d 1 /f
reg add ""HKCU\System\GameConfigStore"" /v GameDVR_EFSEFeatureFlags /t REG_DWORD /d 0 /f
reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\GameDVR"" /v AllowGameDVR /t REG_DWORD /d 0 /f
reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Search"" /v BingSearchEnabled /t REG_DWORD /d 0 /f
reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers"" /v ""HwSchMode"" /t REG_DWORD /d 2 /f
reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize /v EnableTransparency /t REG_DWORD /d 0 /f
reg add ""HKCU\Control Panel\Mouse"" /v MouseSpeed /t REG_SZ /d 0 /f
reg add ""HKCU\Control Panel\Mouse"" /v MouseThreshold1 /t REG_SZ /d 0 /f
reg add ""HKCU\Control Panel\Mouse"" /v MouseThreshold2 /t REG_SZ /d 0 /f
reg add ""HKLM\System\CurrentControlSet\Control\Session Manager\Power"" /v HibernateEnabled /t REG_DWORD /d 0 /f
reg add ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FlyoutMenuSettings"" /v ShowHibernateOption /t REG_DWORD /d 0 /f
powercfg.exe /hibernate off
reg add ""HKLM\SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters"" /v DisabledComponents /t REG_DWORD /d 1 /f
reg add ""HKLM\SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters"" /v ""DisabledComponents"" /t REG_DWORD /d 255 /f
powershell -Command ""Disable-NetAdapterBinding -Name '*' -ComponentID ms_tcpip6"" >nul 2>&1
sc config AJRouter start=disabled
sc config ALG start=demand
sc config AppIDSvc start=demand >nul 2>&1 
sc config AppMgmt start=demand >nul 2>&1 
sc config AppReadiness start=demand
sc config AppVClient start=disabled >nul 2>&1 
sc config AppXSvc start=demand >nul 2>&1 
sc config Appinfo start=demand
sc config AssignedAccessManagerSvc start=disabled >nul 2>&1 
sc config AudioEndpointBuilder start=auto
sc config AudioSrv start=auto
sc config Audiosrv start=auto
sc config AxInstSV start=demand
sc config BDESVC start=demand >nul 2>&1 
sc config BFE start=auto >nul 2>&1 
sc config BITS start=delayed-auto
sc config BTAGService start=demand
sc config BcastDVRUserService_dc2a4 start=demand >nul 2>&1           
sc config BluetoothUserService_dc2a4 start=demand >nul 2>&1 
sc config BrokerInfrastructure start=auto >nul 2>&1 
sc config Browser start=demand >nul 2>&1 
sc config BthAvctpSvc start=auto
sc config BthHFSrv start=auto >nul 2>&1 
sc config CDPSvc start=demand
sc config CDPUserSvc_dc2a4 start=auto >nul 2>&1 
sc config COMSysApp start=demand
sc config CaptureService_dc2a4 start=demand >nul 2>&1 
sc config CertPropSvc start=demand
sc config ClipSVC start=demand >nul 2>&1 
sc config ConsentUxUserSvc_dc2a4 start=demand >nul 2>&1 
sc config CoreMessagingRegistrar start=auto >nul 2>&1 
sc config CredentialEnrollmentManagerUserSvc_dc2a4 start=demand >nul 2>&1 
sc config CryptSvc start=auto
sc config CscService start=demand >nul 2>&1 
sc config DPS start=auto
sc config DcomLaunch start=auto >nul 2>&1 
sc config DcpSvc start=demand >nul 2>&1 
sc config DevQueryBroker start=demand
sc config DeviceAssociationBrokerSvc_dc2a4 start=demand >nul 2>&1 
sc config DeviceAssociationService start=demand
sc config DeviceInstall start=demand
sc config DevicePickerUserSvc_dc2a4 start=demand >nul 2>&1 
sc config DevicesFlowUserSvc_dc2a4 start=demand >nul 2>&1 
sc config Dhcp start=auto
sc config DiagTrack start=disabled
sc config DialogBlockingService start=disabled >nul 2>&1 
sc config DispBrokerDesktopSvc start=auto
sc config DisplayEnhancementService start=demand
sc config DmEnrollmentSvc start=demand
sc config Dnscache start=auto >nul 2>&1 
sc config DoSvc start=delayed-auto >nul 2>&1 
sc config DsSvc start=demand
sc config DsmSvc start=demand
sc config DusmSvc start=auto
sc config EFS start=demand
sc config EapHost start=demand
sc config EntAppSvc start=demand >nul 2>&1 
sc config EventLog start=auto
sc config EventSystem start=auto
sc config FDResPub start=demand
sc config Fax start=demand >nul 2>&1 
sc config FontCache start=auto
sc config FrameServer start=demand
sc config FrameServerMonitor start=demand
sc config GraphicsPerfSvc start=demand
sc config HomeGroupListener start=demand >nul 2>&1 
sc config HomeGroupProvider start=demand >nul 2>&1 
sc config HvHost start=demand
sc config IEEtwCollectorService start=demand >nul 2>&1 
sc config IKEEXT start=demand
sc config InstallService start=demand
sc config InventorySvc start=demand
sc config IpxlatCfgSvc start=demand
sc config KeyIso start=auto
sc config KtmRm start=demand
sc config LSM start=auto >nul 2>&1 
sc config LanmanServer start=auto
sc config LanmanWorkstation start=auto
sc config LicenseManager start=demand
sc config LxpSvc start=demand
sc config MSDTC start=demand
sc config MSiSCSI start=demand
sc config MapsBroker start=delayed-auto
sc config McpManagementService start=demand
sc config MessagingService_dc2a4 start=demand >nul 2>&1 
sc config MicrosoftEdgeElevationService start=demand
sc config MixedRealityOpenXRSvc start=demand >nul 2>&1 
sc config MpsSvc start=auto >nul 2>&1 
sc config MsKeyboardFilter start=demand >nul 2>&1 
sc config NPSMSvc_dc2a4 start=demand >nul 2>&1 
sc config NaturalAuthentication start=demand
sc config NcaSvc start=demand
sc config NcbService start=demand
sc config NcdAutoSetup start=demand
sc config NetSetupSvc start=demand
sc config NetTcpPortSharing start=disabled
sc config Netlogon start=demand
sc config Netman start=demand
sc config NgcCtnrSvc start=demand >nul 2>&1 
sc config NgcSvc start=demand >nul 2>&1 
sc config NlaSvc start=demand
sc config OneSyncSvc_dc2a4 start=auto >nul 2>&1 
sc config P9RdrService_dc2a4 start=demand >nul 2>&1 
sc config PNRPAutoReg start=demand
sc config PNRPsvc start=demand
sc config PcaSvc start=demand
sc config PeerDistSvc start=demand >nul 2>&1 
sc config PenService_dc2a4 start=demand >nul 2>&1  
sc config PerfHost start=demand
sc config PhoneSvc start=demand
sc config PimIndexMaintenanceSvc_dc2a4 start=demand >nul 2>&1 
sc config PlugPlay start=demand
sc config PolicyAgent start=demand
sc config Power start=auto
sc config PrintNotify start=demand
sc config PrintWorkflowUserSvc_dc2a4 start=demand >nul 2>&1 
sc config ProfSvc start=auto
sc config PushToInstall start=demand
sc config QWAVE start=demand
sc config RasAuto start=demand
sc config RasMan start=demand
sc config RemoteAccess start=disabled
sc config RemoteRegistry start=disabled
sc config RetailDemo start=demand
sc config RmSvc start=demand
sc config RpcEptMapper start=auto >nul 2>&1 
sc config RpcLocator start=demand
sc config RpcSs start=auto >nul 2>&1 
sc config SCPolicySvc start=demand
sc config SCardSvr start=demand
sc config SDRSVC start=demand
sc config SEMgrSvc start=demand
sc config SENS start=auto
sc config SNMPTRAP start=demand
sc config SNMPTrap start=demand
sc config SSDPSRV start=demand
sc config SamSs start=auto
sc config ScDeviceEnum start=demand
sc config Schedule start=auto >nul 2>&1 
sc config SecurityHealthService start=demand >nul 2>&1 
sc config Sense start=demand >nul 2>&1 
sc config SensorDataService start=demand
sc config SensorService start=demand
sc config SensrSvc start=demand
sc config SessionEnv start=demand
sc config SgrmBroker start=auto >nul 2>&1 
sc config SharedAccess start=demand
sc config SharedRealitySvc start=demand
sc config ShellHWDetection start=auto
sc config SmsRouter start=demand
sc config Spooler start=auto
sc config SstpSvc start=demand
sc config StateRepository start=demand >nul 2>&1 
sc config StiSvc start=demand
sc config StorSvc start=demand
sc config SysMain start=auto
sc config SystemEventsBroker start=auto >nul 2>&1 
sc config TabletInputService start=demand >nul 2>&1 
sc config TapiSrv start=demand
sc config TermService start=auto
sc config TextInputManagementService start=demand >nul 2>&1 
sc config Themes start=auto
sc config TieringEngineService start=demand
sc config TimeBroker start=demand >nul 2>&1 
sc config TimeBrokerSvc start=demand >nul 2>&1 
sc config TokenBroker start=demand
sc config TrkWks start=auto
sc config TroubleshootingSvc start=demand
sc config TrustedInstaller start=demand
sc config UI0Detect start=demand >nul 2>&1 
sc config UdkUserSvc_dc2a4 start=demand >nul 2>&1 
sc config UevAgentService start=disabled >nul 2>&1 
sc config UmRdpService start=demand
sc config UnistoreSvc_dc2a4 start=demand >nul 2>&1 
sc config UserDataSvc_dc2a4 start=demand >nul 2>&1 
sc config UserManager start=auto
sc config UsoSvc start=demand
sc config VGAuthService start=auto >nul 2>&1 
sc config VMTools start=auto >nul 2>&1 
sc config VSS start=demand
sc config VacSvc start=demand
sc config VaultSvc start=auto
sc config W32Time start=demand
sc config WEPHOSTSVC start=demand
sc config WFDSConMgrSvc start=demand
sc config WMPNetworkSvc start=demand >nul 2>&1 
sc config WManSvc start=demand
sc config WPDBusEnum start=demand
sc config WSService start=demand >nul 2>&1 
sc config WSearch start=delayed-auto
sc config WaaSMedicSvc start=demand >nul 2>&1 
sc config WalletService start=demand
sc config WarpJITSvc start=demand
sc config WbioSrvc start=demand
sc config Wcmsvc start=auto
sc config WcsPlugInService start=demand >nul 2>&1 
sc config WdNisSvc start=demand >nul 2>&1 
sc config WdiServiceHost start=demand
sc config WdiSystemHost start=demand
sc config WebClient start=demand
sc config Wecsvc start=demand
sc config WerSvc start=demand
sc config WiaRpc start=demand
sc config WinDefend start=auto >nul 2>&1
sc config WinHttpAutoProxySvc start=demand >nul 2>&1 
sc config WinRM start=demand
sc config Winmgmt start=auto
sc config WlanSvc start=auto
sc config WpcMonSvc start=demand
sc config WpnService start=demand
sc config WpnUserService_dc2a4 start=auto >nul 2>&1 
sc config WwanSvc start=demand
sc config XblAuthManager start=demand
sc config XblGameSave start=demand
sc config XboxGipSvc start=demand
sc config XboxNetApiSvc start=demand
sc config autotimesvc start=demand
sc config bthserv start=demand
sc config camsvc start=demand
sc config cbdhsvc_dc2a4 start=demand >nul 2>&1 
sc config cloudidsvc start=demand >nul 2>&1 
sc config dcsvc start=demand
sc config defragsvc start=demand
sc config diagnosticshub.standardcollector.service start=demand
sc config diagsvc start=demand
sc config dmwappushservice start=demand
sc config dot3svc start=demand
sc config edgeupdate start=demand
sc config edgeupdatem start=demand
sc config embeddedmode start=demand >nul 2>&1 
sc config fdPHost start=demand
sc config fhsvc start=demand
sc config gpsvc start=auto >nul 2>&1 
sc config hidserv start=demand
sc config icssvc start=demand
sc config iphlpsvc start=auto
sc config lfsvc start=demand
sc config lltdsvc start=demand
sc config lmhosts start=demand
sc config mpssvc start=auto >nul 2>&1 
sc config msiserver start=demand >nul 2>&1 
sc config netprofm start=demand
sc config nsi start=auto
sc config p2pimsvc start=demand
sc config p2psvc start=demand
sc config perceptionsimulation start=demand
sc config pla start=demand
sc config seclogon start=demand
sc config shpamsvc start=disabled
sc config smphost start=demand
sc config spectrum start=demand
sc config sppsvc start=delayed-auto >nul 2>&1 
sc config ssh-agent start=disabled
sc config svsvc start=demand
sc config swprv start=demand
sc config tiledatamodelsvc start=auto >nul 2>&1 
sc config tzautoupdate start=disabled
sc config uhssvc start=disabled >nul 2>&1 
sc config upnphost start=demand
sc config vds start=demand
sc config vm3dservice start=demand >nul 2>&1 
sc config vmicguestinterface start=demand
sc config vmicheartbeat start=demand
sc config vmickvpexchange start=demand
sc config vmicrdv start=demand
sc config vmicshutdown start=demand
sc config vmictimesync start=demand
sc config vmicvmsession start=demand
sc config vmicvss start=demand
sc config vmvss start=demand >nul 2>&1 
sc config wbengine start=demand
sc config wcncsvc start=demand
sc config webthreatdefsvc start=demand
sc config webthreatdefusersvc_dc2a4 start=auto >nul 2>&1 
sc config wercplsupport start=demand
sc config wisvc start=demand
sc config wlidsvc start=demand
sc config wlpasvc start=demand
sc config wmiApSrv start=demand
sc config workfolderssvc start=demand
sc config wscsvc start=delayed-auto >nul 2>&1 
sc config wuauserv start=demand
sc config wudfsvc start=demand >nul 2>&1       
echo Services Set to manual successfully.
schtasks /Change /TN ""Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser"" /Disable >nul 2>&1 
schtasks /Change /TN ""Microsoft\Windows\Application Experience\ProgramDataUpdater"" /Disable >nul 2>&1 
schtasks /Change /TN ""Microsoft\Windows\Autochk\Proxy"" /Disable >nul 2>&1 
schtasks /Change /TN ""Microsoft\Windows\Customer Experience Improvement Program\Consolidator"" /Disable >nul 2>&1 
schtasks /Change /TN ""Microsoft\Windows\Customer Experience Improvement Program\UsbCeip"" /Disable >nul 2>&1 
schtasks /Change /TN ""Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector"" /Disable >nul 2>&1 
schtasks /Change /TN ""Microsoft\Windows\Feedback\Siuf\DmClient"" /Disable >nul 2>&1 
schtasks /Change /TN ""Microsoft\Windows\Feedback\Siuf\DmClientOnScenarioDownload"" /Disable >nul 2>&1 
schtasks /Change /TN ""Microsoft\Windows\Windows Error Reporting\QueueReporting"" /Disable >nul 2>&1 
schtasks /Change /TN ""Microsoft\Windows\Application Experience\MareBackup"" /Disable >nul 2>&1 
schtasks /Change /TN ""Microsoft\Windows\Application Experience\StartupAppTask"" /Disable >nul 2>&1 
schtasks /Change /TN ""Microsoft\Windows\Application Experience\PcaPatchDbTask"" /Disable >nul 2>&1 
schtasks /Change /TN ""Microsoft\Windows\Maps\MapsUpdateTask"" /Disable >nul 2>&1 
reg add ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection"" /v AllowTelemetry /t REG_DWORD /d 0 /f
reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v AllowTelemetry /t REG_DWORD /d 0 /f
reg add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v ContentDeliveryAllowed /t REG_DWORD /d 0 /f
reg add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v OemPreInstalledAppsEnabled /t REG_DWORD /d 0 /f
reg add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v PreInstalledAppsEnabled /t REG_DWORD /d 0 /f
reg add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v PreInstalledAppsEverEnabled /t REG_DWORD /d 0 /f
reg add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SilentInstalledAppsEnabled /t REG_DWORD /d 0 /f
reg add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SubscribedContent-338387Enabled /t REG_DWORD /d 0 /f
reg add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SubscribedContent-338388Enabled /t REG_DWORD /d 0 /f
reg add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SubscribedContent-338389Enabled /t REG_DWORD /d 0 /f
reg add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SubscribedContent-353698Enabled /t REG_DWORD /d 0 /f
reg add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SystemPaneSuggestionsEnabled /t REG_DWORD /d 0 /f
reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent"" /v DisableWindowsConsumerFeatures /t REG_DWORD /d 1 /f
reg add ""HKCU\SOFTWARE\Microsoft\Siuf\Rules"" /v NumberOfSIUFInPeriod /t REG_DWORD /d 0 /f
reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v DoNotShowFeedbackNotifications /t REG_DWORD /d 1 /f
reg add ""HKCU\SOFTWARE\Policies\Microsoft\Windows\CloudContent"" /v DisableTailoredExperiencesWithDiagnosticData /t REG_DWORD /d 1 /f
reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo"" /v DisabledByGroupPolicy /t REG_DWORD /d 1 /f
reg add ""HKLM\SOFTWARE\Microsoft\Windows\Windows Error Reporting"" /v Disabled /t REG_DWORD /d 1 /f
reg add ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config"" /v DODownloadMode /t REG_DWORD /d 1 /f
reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Remote Assistance"" /v fAllowToGetHelp /t REG_DWORD /d 0 /f
reg add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\OperationStatusManager"" /v EnthusiastMode /t REG_DWORD /d 1 /f
reg add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v ShowTaskViewButton /t REG_DWORD /d 0 /f
reg add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced\People"" /v PeopleBand /t REG_DWORD /d 0 /f
reg add ""HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v LaunchTo /t REG_DWORD /d 1 /f
reg add ""HKLM\SYSTEM\CurrentControlSet\Control\FileSystem"" /v LongPathsEnabled /t REG_DWORD /d 1 /f
reg add ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\DriverSearching"" /v SearchOrderConfig /t REG_DWORD /d 1 /f
reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"" /v SystemResponsiveness /t REG_DWORD /d 0 /f
reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"" /v NetworkThrottlingIndex /t REG_DWORD /d 4294967295 /f
reg add ""HKCU\Control Panel\Desktop"" /v MenuShowDelay /t REG_DWORD /d 1 /f
reg add ""HKCU\Control Panel\Desktop"" /v AutoEndTasks /t REG_DWORD /d 1 /f
reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v ClearPageFileAtShutdown /t REG_DWORD /d 0 /f
reg add ""HKLM\SYSTEM\ControlSet001\Services\Ndu"" /v Start /t REG_DWORD /d 2 /f
reg add ""HKCU\Control Panel\Mouse"" /v MouseHoverTime /t REG_SZ /d 400 /f
reg add ""HKLM\SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters"" /v IRPStackSize /t REG_DWORD /d 30 /f
reg add ""HKCU\SOFTWARE\Policies\Microsoft\Windows\Windows Feeds"" /v EnableFeeds /t REG_DWORD /d 0 /f
reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Feeds"" /v ShellFeedsTaskbarViewMode /t REG_DWORD /d 2 /f
reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v HideSCAMeetNow /t REG_DWORD /d 1 /f
reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"" /v ""GPU Priority"" /t REG_DWORD /d 8 /f
reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"" /v Priority /t REG_DWORD /d 6 /f
reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"" /v ""Scheduling Category"" /t REG_SZ /d High /f
reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\UserProfileEngagement"" /v ""ScoobeSystemSettingEnabled"" /t REG_DWORD /d 0 /f
        ";
            await Task.Run(() => BatchCommands(commands));
        }

        private void extra_CheckedChanged(object sender, EventArgs e)
        {
            if (extra.Checked) {
                _ = Extra();
            }
        }

        private async Task Extra()
        {
            string commands = @"
reg add ""HKLM\System\CurrentControlSet\Services\PimIndexMaintenanceSvc"" /v ""Start"" /t REG_DWORD /d ""4"" /f
reg add ""HKLM\System\CurrentControlSet\Services\WinHttpAutoProxySvc"" /v ""Start"" /t REG_DWORD /d ""4"" /f
reg add ""HKLM\System\CurrentControlSet\Services\BcastDVRUserService"" /v ""Start"" /t REG_DWORD /d ""4"" /f
reg add ""HKLM\System\CurrentControlSet\Services\xbgm"" /v ""Start"" /t REG_DWORD /d ""4"" /f
reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\GameDVR"" /v ""AppCaptureEnabled"" /t REG_DWORD /d ""0"" /f
reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\GameDVR"" /v ""AudioCaptureEnabled"" /t REG_DWORD /d ""0"" /f
reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\GameDVR"" /v ""CursorCaptureEnabled"" /t REG_DWORD /d ""0"" /f
reg add ""HKCU\Software\Microsoft\Windows\CurrentVersion\GameDVR"" /v ""MicrophoneCaptureEnabled"" /t REG_DWORD /d ""0"" /f
reg add ""HKCU\System\GameConfigStore"" /v ""GameDVR_FSEBehavior"" /t REG_DWORD /d ""2"" /f
reg add ""HKCU\System\GameConfigStore"" /v ""GameDVR_HonorUserFSEBehaviorMode"" /t REG_DWORD /d ""2"" /f
reg add ""HKCU\System\GameConfigStore"" /v ""GameDVR_Enabled"" /t REG_DWORD /d ""0"" /f
reg add ""HKLM\Software\Policies\Microsoft\Windows\GameDVR"" /v ""AllowgameDVR"" /t REG_DWORD /d ""0"" /f
reg add ""HKCU\Software\Microsoft\GameBar"" /v ""AutoGameModeEnabled"" /t REG_DWORD /d ""0"" /f
sc config wlidsvc start= disabled
sc config DisplayEnhancementService start= disabled
sc config DiagTrack start= disabled
sc config DusmSvc start= disabled
sc config TabletInputService start= disabled >nul 2>&1
sc config RetailDemo start= disabled
sc config Fax start= disabled >nul 2>&1
sc config SharedAccess start= disabled
sc config lfsvc start= disabled
sc config WpcMonSvc start= disabled
sc config SessionEnv start= disabled
sc config MicrosoftEdgeElevationService start= disabled
sc config edgeupdate start= disabled
sc config edgeupdatem start= disabled
sc config autotimesvc start= disabled
sc config CscService start= disabled >nul 2>&1
sc config TermService start= disabled
sc config SensorDataService start= disabled
sc config SensorService start= disabled
sc config SensrSvc start= disabled
sc config shpamsvc start= disabled
sc config diagnosticshub.standardcollector.service start= disabled
sc config PhoneSvc start= disabled
sc config TapiSrv start= disabled
sc config UevAgentService start= disabled >nul 2>&1
sc config WalletService start= disabled
sc config TokenBroker start= disabled
sc config WebClient start= disabled
sc config MixedRealityOpenXRSvc start= disabled >nul 2>&1
sc config stisvc start= disabled
sc config WbioSrvc start= disabled
sc config icssvc start= disabled
sc config Wecsvc start= disabled
sc config XboxGipSvc start= disabled
sc config XblAuthManager start= disabled
sc config XboxNetApiSvc start= disabled
sc config XblGameSave start= disabled
sc config SEMgrSvc start= disabled
sc config iphlpsvc start= disabled
sc config Backupper Service start= disabled >nul 2>&1
sc config BthAvctpSvc start= disabled
sc config BDESVC start= disabled >nul 2>&1
sc config cbdhsvc start= disabled
sc config CDPSvc start= disabled
sc config CDPUserSvc start= disabled
sc config DevQueryBroker start= disabled
sc config DevicesFlowUserSvc start= disabled
sc config dmwappushservice start= disabled
sc config DispBrokerDesktopSvc start= disabled
sc config TrkWks start= disabled
sc config dLauncherLoopback start= disabled >nul 2>&1
sc config EFS start= disabled
sc config fdPHost start= disabled
sc config FDResPub start= disabled
sc config IKEEXT start= disabled
sc config NPSMSvc start= disabled
sc config WPDBusEnum start= disabled
sc config PcaSvc start= disabled
sc config RasMan start= disabled
sc config RetailDemo start=disabled
sc config SstpSvc start=disabled
sc config ShellHWDetection start= disabled
sc config SSDPSRV start= disabled
sc config SysMain start= disabled
sc config OneSyncSvc start= disabled
sc config lmhosts start= disabled
sc config UserDataSvc start= disabled
sc config UnistoreSvc start= disabled
sc config Wcmsvc start= disabled
sc config FontCache start= disabled
sc config W32Time start= disabled
sc config tzautoupdate start= disabled
sc config DsSvc start= disabled
sc config DevicesFlowUserSvc_5f1ad start= disabled >nul 2>&1
sc config diagsvc start= disabled
sc config DialogBlockingService start= disabled >nul 2>&1
sc config PimIndexMaintenanceSvc_5f1ad start= disabled >nul 2>&1
sc config MessagingService_5f1ad start= disabled >nul 2>&1
sc config AppVClient start= disabled >nul 2>&1
sc config MsKeyboardFilter start= disabled >nul 2>&1
sc config NetTcpPortSharing start= disabled
sc config ssh-agent start= disabled
sc config SstpSvc start= disabled
sc config OneSyncSvc_5f1ad start= disabled >nul 2>&1
sc config wercplsupport start= disabled
sc config WMPNetworkSvc start= disabled >nul 2>&1
sc config WerSvc start= disabled
sc config WpnUserService_5f1ad start= disabled >nul 2>&1
sc config WinHttpAutoProxySvc start= disabled >nul 2>&1
schtasks /DELETE /TN ""AMDInstallLauncher"" /f >nul 2>&1
schtasks /DELETE /TN ""AMDLinkUpdate"" /f >nul 2>&1
schtasks /DELETE /TN ""AMDRyzenMasterSDKTask"" /f >nul 2>&1
schtasks /DELETE /TN ""Driver Easy Scheduled Scan"" /f >nul 2>&1
schtasks /DELETE /TN ""ModifyLinkUpdate"" /f >nul 2>&1
schtasks /DELETE /TN ""SoftMakerUpdater"" /f >nul 2>&1
schtasks /DELETE /TN ""StartCN"" /f >nul 2>&1
schtasks /DELETE /TN ""StartDVR"" /f >nul 2>&1
schtasks /Change /TN ""Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser"" /Disable >nul 2>&1
schtasks /Change /TN ""Microsoft\Windows\Application Experience\PcaPatchDbTask"" /Disable >nul 2>&1
schtasks /Change /TN ""Microsoft\Windows\Application Experience\ProgramDataUpdater"" /Disable >nul 2>&1
schtasks /Change /TN ""Microsoft\Windows\Application Experience\StartupAppTask"" /Disable >nul 2>&1
schtasks /Change /TN ""Microsoft\Windows\Autochk\Proxy"" /Disable >nul 2>&1
schtasks /Change /TN ""Microsoft\Windows\Customer Experience Improvement Program\Consolidator"" /Disable >nul 2>&1
schtasks /Change /TN ""Microsoft\Windows\Customer Experience Improvement Program\UsbCeip"" /Disable >nul 2>&1
schtasks /Change /TN ""Microsoft\Windows\Defrag\ScheduledDefrag"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Device Information\Device"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Device Information\Device User"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Diagnosis\RecommendedTroubleshootingScanner"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Diagnosis\Scheduled"" /Disable
schtasks /Change /TN ""Microsoft\Windows\DiskCleanup\SilentCleanup"" /Disable
schtasks /Change /TN ""Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector"" /Disable >nul 2>&1
schtasks /Change /TN ""Microsoft\Windows\DiskFootprint\Diagnostics"" /Disable
schtasks /Change /TN ""Microsoft\Windows\DiskFootprint\StorageSense"" /Disable
schtasks /Change /TN ""Microsoft\Windows\DUSM\dusmtask"" /Disable
schtasks /Change /TN ""Microsoft\Windows\EnterpriseMgmt\MDMMaintenenceTask"" /Disable >nul 2>&1
schtasks /Change /TN ""Microsoft\Windows\Feedback\Siuf\DmClient"" /Disable >nul 2>&1
schtasks /Change /TN ""Microsoft\Windows\Feedback\Siuf\DmClientOnScenarioDownload"" /Disable >nul 2>&1
schtasks /Change /TN ""Microsoft\Windows\FileHistory\File History (maintenance mode)"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Flighting\FeatureConfig\ReconcileFeatures"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Flighting\FeatureConfig\UsageDataFlushing"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Flighting\FeatureConfig\UsageDataReporting"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Flighting\OneSettings\RefreshCache"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Input\LocalUserSyncDataAvailable"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Input\MouseSyncDataAvailable"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Input\PenSyncDataAvailable"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Input\TouchpadSyncDataAvailable"" /Disable
schtasks /Change /TN ""Microsoft\Windows\International\Synchronize Language Settings"" /Disable
schtasks /Change /TN ""Microsoft\Windows\LanguageComponentsInstaller\Installation"" /Disable
schtasks /Change /TN ""Microsoft\Windows\LanguageComponentsInstaller\ReconcileLanguageResources"" /Disable
schtasks /Change /TN ""Microsoft\Windows\LanguageComponentsInstaller\Uninstallation"" /Disable >nul 2>&1
schtasks /Change /TN ""Microsoft\Windows\License Manager\TempSignedLicenseExchange"" /Disable >nul 2>&1
schtasks /Change /TN ""Microsoft\Windows\License Manager\TempSignedLicenseExchange"" /Disable >nul 2>&1
schtasks /Change /TN ""Microsoft\Windows\Management\Provisioning\Cellular"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Management\Provisioning\Logon"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Maintenance\WinSAT"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Maps\MapsToastTask"" /Disable >nul 2>&1
schtasks /Change /TN ""Microsoft\Windows\Maps\MapsUpdateTask"" /Disable >nul 2>&1
schtasks /Change /TN ""Microsoft\Windows\Mobile Broadband Accounts\MNO Metadata Parser"" /Disable >nul 2>&1
schtasks /Change /TN ""Microsoft\Windows\MUI\LPRemove"" /Disable
schtasks /Change /TN ""Microsoft\Windows\NetTrace\GatherNetworkInfo"" /Disable
schtasks /Change /TN ""Microsoft\Windows\PI\Sqm-Tasks"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Power Efficiency Diagnostics\AnalyzeSystem"" /Disable
schtasks /Change /TN ""Microsoft\Windows\PushToInstall\Registration"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Ras\MobilityManager"" /Disable
schtasks /Change /TN ""Microsoft\Windows\RecoveryEnvironment\VerifyWinRE"" /Disable
schtasks /Change /TN ""Microsoft\Windows\RemoteAssistance\RemoteAssistanceTask"" /Disable >nul 2>&1
schtasks /Change /TN ""Microsoft\Windows\RetailDemo\CleanupOfflineContent"" /Disable >nul 2>&1
schtasks /Change /TN ""Microsoft\Windows\Servicing\StartComponentCleanup"" /Disable
schtasks /Change /TN ""Microsoft\Windows\SettingSync\NetworkStateChangeTask"" /Disable >nul 2>&1
schtasks /Change /TN ""Microsoft\Windows\Setup\SetupCleanupTask"" /Disable >nul 2>&1
schtasks /Change /TN ""Microsoft\Windows\Setup\SnapshotCleanupTask"" /Disable >nul 2>&1
schtasks /Change /TN ""Microsoft\Windows\SpacePort\SpaceAgentTask"" /Disable
schtasks /Change /TN ""Microsoft\Windows\SpacePort\SpaceManagerTask"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Speech\SpeechModelDownloadTask"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Storage Tiers Management\Storage Tiers Management Initialization"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Sysmain\ResPriStaticDbSync"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Sysmain\WsSwapAssessmentTask"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Task Manager\Interactive"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Time Synchronization\ForceSynchronizeTime"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Time Synchronization\SynchronizeTime"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Time Zone\SynchronizeTimeZone"" /Disable
schtasks /Change /TN ""Microsoft\Windows\TPM\Tpm-HASCertRetr"" /Disable
schtasks /Change /TN ""Microsoft\Windows\TPM\Tpm-Maintenance"" /Disable
schtasks /Change /TN ""Microsoft\Windows\UPnP\UPnPHostConfig"" /Disable
schtasks /Change /TN ""Microsoft\Windows\User Profile Service\HiveUploadTask"" /Disable >nul 2>&1
schtasks /Change /TN ""Microsoft\Windows\WDI\ResolutionHost"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Windows Filtering Platform\BfeOnServiceStartTypeChange"" /Disable
schtasks /Change /TN ""Microsoft\Windows\WOF\WIM-Hash-Management"" /Disable >nul 2>&1
schtasks /Change /TN ""Microsoft\Windows\WOF\WIM-Hash-Validation"" /Disable >nul 2>&1
schtasks /Change /TN ""Microsoft\Windows\Work Folders\Work Folders Logon Synchronization"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Work Folders\Work Folders Maintenance Work"" /Disable
schtasks /Change /TN ""Microsoft\Windows\Workplace Join\Automatic-Device-Join"" /Disable >nul 2>&1
schtasks /Change /TN ""Microsoft\Windows\WwanSvc\NotificationTask"" /Disable
schtasks /Change /TN ""Microsoft\Windows\WwanSvc\OobeDiscovery"" /Disable
schtasks /Change /TN ""Microsoft\XblGameSave\XblGameSaveTask"" /Disable
        ";
            await Task.Run(() => BatchCommands(commands));
        }

        private void nvidia_CheckedChanged(object sender, EventArgs e)
        {
            if (nvidia.Checked) {
                _ = NVIDIA();
            }
        }

        private async Task NVIDIA()
        {
            string commands = @"
REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{7B7A1E6E-0A7E-11EF-946A-806E6F6E6963}\0000"" /v ""PowerMizerEnable"" /t REG_DWORD /d ""1"" /f 
REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{7B7A1E6E-0A7E-11EF-946A-806E6F6E6963}\0000"" /v ""PowerMizerLevel"" /t REG_DWORD /d ""1"" /f 
REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{7B7A1E6E-0A7E-11EF-946A-806E6F6E6963}\0000"" /v ""PowerMizerLevelAC"" /t REG_DWORD /d ""1"" /f 
REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{7B7A1E6E-0A7E-11EF-946A-806E6F6E6963}\0000"" /v ""PerfLevelSrc"" /t REG_DWORD /d ""8738"" /f
REG ADD ""HKLM\SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\NVTweak"" /v ""DisplayPowerSaving"" /t Reg_DWORD /d ""0"" /f 
REG ADD ""HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e96c-e325-11ce-bfc1-08002be10318}\0001\PowerSettings"" /v IdlePowerState /t REG_BINARY /d 00000000 /f
REG ADD ""HKLM\SYSTEM\CurrentControlSet\Control\Class{4d36e96c-e325-11ce-bfc1-08002be10318}\0000"" /v ""DisableDynamicPstate"" /t REG_DWORD /d ""1"" /f
REG ADD ""HKLM\SOFTWARE\NVIDIA Corporation\NvControlPanel2\Client"" /v ""OptInOrOutPreference"" /t REG_DWORD /d 0 /f 
REG ADD ""HKLM\SOFTWARE\NVIDIA Corporation\Global\FTS"" /v ""EnableRID44231"" /t REG_DWORD /d 0 /f 
REG ADD ""HKLM\SOFTWARE\NVIDIA Corporation\Global\FTS"" /v ""EnableRID64640"" /t REG_DWORD /d 0 /f 
REG ADD ""HKLM\SOFTWARE\NVIDIA Corporation\Global\FTS"" /v ""EnableRID66610"" /t REG_DWORD /d 0 /f 
REG ADD ""HKLM\SYSTEM\CurrentControlSet\Services\nvlddmkm\FTS"" /v ""EnableRID61684"" /t REG_DWORD /d ""1"" /f
REG ADD ""HKLM\SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\Startup"" /v ""SendTelemetryData"" /t REG_DWORD /d 0 /f
REG DELETE ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run"" /v ""NvBackend"" /f >nul 2>&1
REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"" /v ""RMHdcpKeyGlobZero"" /t REG_DWORD /d 1 /f
REG ADD ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\Dwm"" /v ""OverlayTestMode"" /t REG_DWORD /d 5 /f
        ";
            await Task.Run(() => BatchCommands(commands));
        }

        private void amd_CheckedChanged(object sender, EventArgs e)
        {
            if (amd.Checked) {
                _ = AMD();
            }
        }

        private async Task AMD()
        {
            string commands = @"
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000"" /v ""3D_Refresh_Rate_Override_DEF"" /t Reg_DWORD /d ""0"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000"" /v ""3to2Pulldown_NA"" /t Reg_DWORD /d ""0"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000"" /v ""AAF_NA"" /t Reg_DWORD /d ""0"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000"" /v ""Adaptive De-interlacing"" /t Reg_DWORD /d ""1"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000"" /v ""AllowRSOverlay"" /t Reg_SZ /d ""false"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000"" /v ""AllowSkins"" /t Reg_SZ /d ""false"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000"" /v ""AllowSnapshot"" /t Reg_DWORD /d ""0"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000"" /v ""AllowSubscription"" /t Reg_DWORD /d ""0"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000"" /v ""AntiAlias_NA"" /t Reg_SZ /d ""0"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000"" /v ""AreaAniso_NA"" /t Reg_SZ /d ""0"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000"" /v ""ASTT_NA"" /t Reg_SZ /d ""0"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000"" /v ""AutoColorDepthReduction_NA"" /t Reg_DWORD /d ""0"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000"" /v ""DisableSAMUPowerGating"" /t Reg_DWORD /d ""1"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000"" /v ""DisableUVDPowerGatingDynamic"" /t Reg_DWORD /d ""1"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000"" /v ""DisableVCEPowerGating"" /t Reg_DWORD /d ""1"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000"" /v ""EnableAspmL0s"" /t Reg_DWORD /d ""0"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000"" /v ""EnableAspmL1"" /t Reg_DWORD /d ""0"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000"" /v ""EnableUlps"" /t Reg_DWORD /d ""0"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000"" /v ""EnableUlps_NA"" /t Reg_SZ /d ""0"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000"" /v ""KMD_DeLagEnabled"" /t Reg_DWORD /d ""1"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000"" /v ""KMD_FRTEnabled"" /t Reg_DWORD /d ""0"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000"" /v ""DisableDMACopy"" /t Reg_DWORD /d ""1"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000"" /v ""DisableBlockWrite"" /t Reg_DWORD /d ""0"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000"" /v ""StutterMode"" /t Reg_DWORD /d ""0"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000"" /v ""EnableUlps"" /t Reg_DWORD /d ""0"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000"" /v ""PP_SclkDeepSleepDisable"" /t Reg_DWORD /d ""1"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000"" /v ""PP_ThermalAutoThrottlingEnable"" /t Reg_DWORD /d ""0"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000"" /v ""DisableDrmdmaPowerGating"" /t Reg_DWORD /d ""1"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000"" /v ""KMD_EnableComputePreemption"" /t Reg_DWORD /d ""0"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000\UMD"" /t Reg_SZ /d ""1"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000\UMD"" /v ""Main3D"" /t Reg_BINARY /d ""3100"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000\UMD"" /v ""FlipQueueSize"" /t Reg_BINARY /d ""3100"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000\UMD"" /v ""ShaderCache"" /t Reg_BINARY /d ""3200"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000\UMD"" /v ""Tessellation_OPTION"" /t Reg_BINARY /d ""3200"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000\UMD"" /v ""Tessellation"" /t Reg_BINARY /d ""3100"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000\UMD"" /v ""VSyncControl"" /t Reg_BINARY /d ""3000"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000\UMD"" /v ""TFQ"" /t Reg_BINARY /d ""3200"" /f 
    REG ADD ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video\{B784559B-672D-11EE-A4CA-E612636C81AA}\0000\DAL2_DATA__2_0\DisplayPath_4\EDID_D109_78E9\Option"" /v ""ProtectionControl"" /t REG_BINARY /d ""0100000001000000"" /f
        ";
            await Task.Run(() => BatchCommands(commands));
        }

        // Tweaks Tab End

        // Fix Tab
        private void fixwifi_CheckedChanged(object sender, EventArgs e)
        {
            if (fixwifi.Checked) {
                _ = EnableWifi();
            }
        }

        private async Task EnableWifi()
        {
            string commands = @"
sc config LanmanWorkstation start= demand
sc config WdiServiceHost start= demand
sc config NcbService start= demand
sc config ndu start= demand
sc config Netman start= demand
sc config netprofm start= demand
sc config WwanSvc start= demand
sc config Dhcp start= auto
sc config DPS start= auto
sc config lmhosts start= auto
sc config NlaSvc start= auto
sc config nsi start= auto
sc config RmSvc start= auto
sc config Wcmsvc start= auto
sc config Winmgmt start= auto
sc config WlanSvc start= auto
schtasks /Change /TN ""Microsoft\Windows\WlanSvc\CDSSync"" /Enable
schtasks /Change /TN ""Microsoft\Windows\WCM\WiFiTask"" /Enable
schtasks /Change /TN ""Microsoft\Windows\NlaSvc\WiFiTask"" /Enable
schtasks /Change /TN ""Microsoft\Windows\DUSM\dusmtask"" /Enable
reg add ""HKLM\Software\Policies\Microsoft\Windows\NetworkConnectivityStatusIndicator"" /v ""NoActiveProbe"" /t REG_DWORD /d ""0"" /f
reg add ""HKLM\System\CurrentControlSet\Services\NlaSvc\Parameters\Internet"" /v ""EnableActiveProbing"" /t REG_DWORD /d ""1"" /f
reg add ""HKLM\System\CurrentControlSet\Services\BFE"" /v ""Start"" /t REG_DWORD /d ""2"" /f
reg add ""HKLM\System\CurrentControlSet\Services\Dnscache"" /v ""Start"" /t REG_DWORD /d ""2"" /f
reg add ""HKLM\System\CurrentControlSet\Services\WinHttpAutoProxySvc"" /v ""Start"" /t REG_DWORD /d ""3"" /f
net start DPS
net start nsi
net start NlaSvc
net start Dhcp
net start Wcmsvc
net start RmSvc
wmic path win32_networkadapter where index=0 call disable
wmic path win32_networkadapter where index=1 call disable
wmic path win32_networkadapter where index=2 call disable
wmic path win32_networkadapter where index=3 call disable
wmic path win32_networkadapter where index=4 call disable
wmic path win32_networkadapter where index=5 call disable
wmic path win32_networkadapter where index=0 call enable
wmic path win32_networkadapter where index=1 call enable
wmic path win32_networkadapter where index=2 call enable
wmic path win32_networkadapter where index=3 call enable
wmic path win32_networkadapter where index=4 call enable
wmic path win32_networkadapter where index=5 call enable
arp -d *
route -f
nbtstat -R
nbtstat -RR
netcfg -d
netsh winsock reset
netsh int 6to4 reset all
netsh int httpstunnel reset all
netsh int ip reset
netsh int isatap reset all
netsh int portproxy reset all
netsh int tcp reset all
netsh int teredo reset all
netsh branchcache reset
ipconfig /release
ipconfig /renew
sc config WlanSvc start= auto
sc config Wcmsvc start= auto
        ";
            await Task.Run(() => BatchCommands(commands));
        }

        private void fixbluethooth_CheckedChanged(object sender, EventArgs e)
        {
            if (fixbluethooth.Checked) {
                _ = FixBluethooth();
            }
        }

        private async Task FixBluethooth()
        {
            string commands = @"
sc config RFCOMM start=demand
sc config BthEnum start=demand
sc config bthleenum start=demand
sc config BTHMODEM start=demand
sc config BthA2dp start=demand
sc config microsoft_bluetooth_avrcptransport start=demand
sc config BthHFEnum start=demand
sc config BTAGService start=demand
sc config bthserv start=demand
sc config BluetoothUserService start=demand
sc config BthAvctpSvc start=demand
Reg.exe add ""HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Services\BTAGService"" /v ""Start"" /t REG_DWORD /d ""2"" /f
Reg.exe add ""HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Services\bthserv"" /v ""Start"" /t REG_DWORD /d ""2"" /f
Reg.exe add ""HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Services\BthAvctpSvc"" /v ""Start"" /t REG_DWORD /d ""2"" /f
Reg.exe add ""HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Services\BluetoothUserService"" /v ""Start"" /t REG_DWORD /d ""2"" /f
        ";
            await Task.Run(() => BatchCommands(commands));
        }

        private void fixmsstore_CheckedChanged(object sender, EventArgs e)
        {
            if (fixmsstore.Checked) {
                _ = DownloadMsStore();
                _ = FixMsStore();
            }
        }

        private async Task DownloadMsStore()
        {
            string commands = @"
Get-AppxPackage -allusers Microsoft.WindowsStore | Foreach {Add-AppxPackage -DisableDevelopmentMode -Register ""$($_.InstallLocation)\AppXManifest.xml""}       
            ";
            await Task.Run(() => PowerShellCommands(commands));
        }

        private async Task FixMsStore()
        {
            string commands = @"
reg add ""HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Services\iphlpsvc"" /v ""Start"" /t REG_DWORD /d ""2"" /f
reg add ""HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Services\ClipSVC"" /v ""Start"" /t REG_DWORD /d ""2"" /f
reg add ""HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Services\AppXSvc"" /v ""Start"" /t REG_DWORD /d ""2"" /f
reg add ""HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Services\LicenseManager"" /v ""Start"" /t REG_DWORD /d ""2"" /f
reg add ""HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Services\NgcSvc"" /v ""Start"" /t REG_DWORD /d ""2"" /f
reg add ""HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Services\NgcCtnrSvc"" /v ""Start"" /t REG_DWORD /d ""2"" /f
reg add ""HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Services\wlidsvc"" /v ""Start"" /t REG_DWORD /d ""2"" /f
reg add ""HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Services\TokenBroker"" /v ""Start"" /t REG_DWORD /d ""2"" /f
reg add ""HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Services\WalletService"" /v ""Start"" /t REG_DWORD /d ""2"" /f
reg add ""HKLM\SOFTWARE\Policies\Microsoft\WindowsStore"" /v ""DisableStoreApps"" /t REG_DWORD /d ""0"" /f
reg add ""HKLM\SOFTWARE\Policies\Microsoft\WindowsStore"" /v ""RemoveWindowsStore"" /t REG_DWORD /d ""0"" /f
reg add ""HKLM\SYSTEM\CurrentControlSet\Services\DoSvc"" /v ""Start"" /t REG_DWORD /d ""2"" /f
        ";
            await Task.Run(() => BatchCommands(commands));
        }

        private void fixmicrophone_CheckedChanged(object sender, EventArgs e)
        {
            if (fixmicrophone.Checked) {
                _ = FixMic();
            }
        }

        private async Task FixMic()
        {
            string commands = @"
setlocal EnableExtensions DisableDelayedExpansion
PowerShell -ExecutionPolicy Unrestricted -Command ""reg delete 'HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy' /v 'LetAppsAccessMicrophone' /f 2>$null""
PowerShell -ExecutionPolicy Unrestricted -Command ""reg delete 'HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy' /v 'LetAppsAccessMicrophone_UserInControlOfTheseApps' /f 2>$null""
PowerShell -ExecutionPolicy Unrestricted -Command ""reg delete 'HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy' /v 'LetAppsAccessMicrophone_ForceAllowTheseApps' /f 2>$null""
PowerShell -ExecutionPolicy Unrestricted -Command ""reg delete 'HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy' /v 'LetAppsAccessMicrophone_ForceDenyTheseApps' /f 2>$null""
PowerShell -ExecutionPolicy Unrestricted -Command ""$revertData = 'Allow'; reg add 'HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone' /v 'Value' /t 'REG_SZ' /d ""^""""$revertData""^"""" /f""
PowerShell -ExecutionPolicy Unrestricted -Command ""reg delete 'HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\DeviceAccess\Global\{2EEF81BE-33FA-4800-9670-1CD474972C3F}' /v 'Value' /f 2>$null""
endlocal
        ";
            await Task.Run(() => BatchCommands(commands));
        }

        private void activatewindows_CheckedChanged(object sender, EventArgs e)
        {
            if (activatewindows.Checked) {
                _ = ActivateWindows();
            }
        }

        private async Task ActivateWindows()
        {
            string commands = @"
Powershell -Command ""irm https://get.activated.win | iex""
        ";
            await Task.Run(() => BatchCommands(commands));
        }

        private void xboxfix_CheckedChanged(object sender, EventArgs e)
        {
            if (xboxfix.Checked) {
                _ = FixXbox();
            }
        }

        private async Task FixXbox()
        {
            string commands = @"
PowerShell -Command ""Get-AppXPackage | ForEach-Object {Add-AppxPackage -DisableDevelopmentMode -Register \""$($_.InstallLocation)\AppXManifest.xml\""}""
reg add ""HKLM\System\CurrentControlSet\Services\PimIndexMaintenanceSvc"" /v ""Start"" /t REG_DWORD /d ""3"" /f
reg add ""HKLM\System\CurrentControlSet\Services\WinHttpAutoProxySvc"" /v ""Start"" /t REG_DWORD /d ""3"" /fd
sc config wlidsvc start= demand
sc config DisplayEnhancementService start= demand
sc config DiagTrack start= demand
sc config DusmSvc start= demand
sc config TabletInputService start= demand
sc config RetailDemo start= demand
sc config Fax start= demand
sc config SharedAccess start= demand
sc config lfsvc start= demand
sc config WpcMonSvc start= demand
sc config SessionEnv start= demand
sc config MicrosoftEdgeElevationService start= demand
sc config edgeupdate start= demand
sc config edgeupdatem start= demand
sc config autotimesvc start= demand
sc config CscService start= demand
sc config TermService start= demand
sc config SensorDataService start= demand
sc config SensorService start= demand
sc config SensrSvc start= demand
sc config shpamsvc start= demand
sc config diagnosticshub.standardcollector.service start= demand
sc config PhoneSvc start= demand
sc config TapiSrv start= demand
sc config UevAgentService start= demand
sc config WalletService start= demand
sc config TokenBroker start= demand
sc config WebClient start= demand
sc config MixedRealityOpenXRSvc start= demand
sc config stisvc start= demand
sc config WbioSrvc start= demand
sc config icssvc start= demand
sc config Wecsvc start= demand
sc config XboxGipSvc start= demand
sc config XblAuthManager start= demand
sc config XboxNetApiSvc start= demand
sc config XblGameSave start= demand
sc config SEMgrSvc start= demand
sc config iphlpsvc start= demand
sc config Backupper Service"" start= demand
sc config BthAvctpSvc start= demand
sc config BDESVC start= demand
sc config cbdhsvc start= demand
sc config CDPSvc start= demand
sc config CDPUserSvc start= demand
sc config DevQueryBroker start= demand
sc config DevicesFlowUserSvc start= demand
sc config dmwappushservice start= demand
sc config DispBrokerDesktopSvc start= demand
sc config TrkWks start= demand
sc config dLauncherLoopback start= demand
sc config EFS start= demand
sc config fdPHost start= demand
sc config FDResPub start= demand
sc config IKEEXT start= demand
sc config NPSMSvc start= demand
sc config WPDBusEnum start= demand
sc config PcaSvc start= demand
sc config RasMan start= demand
sc config RetailDemo start=disabled
sc config SstpSvc start=disabled
sc config ShellHWDetection start= demand
sc config SSDPSRV start= demand
sc config SysMain start= demand
sc config OneSyncSvc start= demand
sc config lmhosts start= demand
sc config UserDataSvc start= demand
sc config UnistoreSvc start= demand
sc config Wcmsvc start= demand
sc config FontCache start= demand
sc config W32Time start= demand
sc config tzautoupdate start= demand
sc config DsSvc start= demand
sc config DevicesFlowUserSvc_5f1ad start= demand
sc config diagsvc start= demand
sc config DialogBlockingService start= demand
sc config PimIndexMaintenanceSvc_5f1ad start= demand
sc config MessagingService_5f1ad start= demand
sc config AppVClient start= demand
sc config MsKeyboardFilter start= demand
sc config NetTcpPortSharing start= demand
sc config ssh-agent start= demand
sc config SstpSvc start= demand
sc config OneSyncSvc_5f1ad start= demand
sc config wercplsupport start= demand
sc config WMPNetworkSvc start= demand
sc config WerSvc start= demand
sc config WpnUserService_5f1ad start= demand
sc config WinHttpAutoProxySvc start= demand
schtasks /Change /TN ""Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Application Experience\PcaPatchDbTask"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Application Experience\ProgramDataUpdater"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Application Experience\StartupAppTask"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Autochk\Proxy"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Customer Experience Improvement Program\Consolidator"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Customer Experience Improvement Program\UsbCeip"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Defrag\ScheduledDefrag"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Device Information\Device"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Device Information\Device User"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Diagnosis\RecommendedTroubleshootingScanner"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Diagnosis\Scheduled"" /Enable
schtasks /Change /TN ""Microsoft\Windows\DiskCleanup\SilentCleanup"" /Enable
schtasks /Change /TN ""Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector"" /Enable
schtasks /Change /TN ""Microsoft\Windows\DiskFootprint\Diagnostics"" /Enable
schtasks /Change /TN ""Microsoft\Windows\DiskFootprint\StorageSense"" /Enable
schtasks /Change /TN ""Microsoft\Windows\DUSM\dusmtask"" /Enable
schtasks /Change /TN ""Microsoft\Windows\EnterpriseMgmt\MDMMaintenenceTask"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Feedback\Siuf\DmClient"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Feedback\Siuf\DmClientOnScenarioDownload"" /Enable
schtasks /Change /TN ""Microsoft\Windows\FileHistory\File History (maintenance mode)"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Flighting\FeatureConfig\ReconcileFeatures"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Flighting\FeatureConfig\UsageDataFlushing"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Flighting\FeatureConfig\UsageDataReporting"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Flighting\OneSettings\RefreshCache"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Input\LocalUserSyncDataAvailable"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Input\MouseSyncDataAvailable"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Input\PenSyncDataAvailable"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Input\TouchpadSyncDataAvailable"" /Enable
schtasks /Change /TN ""Microsoft\Windows\International\Synchronize Language Settings"" /Enable
schtasks /Change /TN ""Microsoft\Windows\LanguageComponentsInstaller\Installation"" /Enable
schtasks /Change /TN ""Microsoft\Windows\LanguageComponentsInstaller\ReconcileLanguageResources"" /Enable
schtasks /Change /TN ""Microsoft\Windows\LanguageComponentsInstaller\Uninstallation"" /Enable
schtasks /Change /TN ""Microsoft\Windows\License Manager\TempSignedLicenseExchange"" /Enable
schtasks /Change /TN ""Microsoft\Windows\License Manager\TempSignedLicenseExchange"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Management\Provisioning\Cellular"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Management\Provisioning\Logon"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Maintenance\WinSAT"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Maps\MapsToastTask"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Maps\MapsUpdateTask"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Mobile Broadband Accounts\MNO Metadata Parser"" /Enable
schtasks /Change /TN ""Microsoft\Windows\MUI\LPRemove"" /Enable
schtasks /Change /TN ""Microsoft\Windows\NetTrace\GatherNetworkInfo"" /Enable
schtasks /Change /TN ""Microsoft\Windows\PI\Sqm-Tasks"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Power Efficiency Diagnostics\AnalyzeSystem"" /Enable
schtasks /Change /TN ""Microsoft\Windows\PushToInstall\Registration"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Ras\MobilityManager"" /Enable
schtasks /Change /TN ""Microsoft\Windows\RecoveryEnvironment\VerifyWinRE"" /Enable
schtasks /Change /TN ""Microsoft\Windows\RemoteAssistance\RemoteAssistanceTask"" /Enable
schtasks /Change /TN ""Microsoft\Windows\RetailDemo\CleanupOfflineContent"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Servicing\StartComponentCleanup"" /Enable
schtasks /Change /TN ""Microsoft\Windows\SettingSync\NetworkStateChangeTask"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Setup\SetupCleanupTask"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Setup\SnapshotCleanupTask"" /Enable
schtasks /Change /TN ""Microsoft\Windows\SpacePort\SpaceAgentTask"" /Enable
schtasks /Change /TN ""Microsoft\Windows\SpacePort\SpaceManagerTask"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Speech\SpeechModelDownloadTask"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Storage Tiers Management\Storage Tiers Management Initialization"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Sysmain\ResPriStaticDbSync"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Sysmain\WsSwapAssessmentTask"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Task Manager\Interactive"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Time Synchronization\ForceSynchronizeTime"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Time Synchronization\SynchronizeTime"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Time Zone\SynchronizeTimeZone"" /Enable
schtasks /Change /TN ""Microsoft\Windows\TPM\Tpm-HASCertRetr"" /Enable
schtasks /Change /TN ""Microsoft\Windows\TPM\Tpm-Maintenance"" /Enable
schtasks /Change /TN ""Microsoft\Windows\UPnP\UPnPHostConfig"" /Enable
schtasks /Change /TN ""Microsoft\Windows\User Profile Service\HiveUploadTask"" /Enable
schtasks /Change /TN ""Microsoft\Windows\WDI\ResolutionHost"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Windows Filtering Platform\BfeOnServiceStartTypeChange"" /Enable
schtasks /Change /TN ""Microsoft\Windows\WOF\WIM-Hash-Management"" /Enable
schtasks /Change /TN ""Microsoft\Windows\WOF\WIM-Hash-Validation"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Work Folders\Work Folders Logon Synchronization"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Work Folders\Work Folders Maintenance Work"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Workplace Join\Automatic-Device-Join"" /Enable
schtasks /Change /TN ""Microsoft\Windows\WwanSvc\NotificationTask"" /Enable
schtasks /Change /TN ""Microsoft\Windows\WwanSvc\OobeDiscovery"" /Enable
sc config uhssvc start= demand
sc config upfc start= demand
sc config PushToInstall start= demand
sc config BITS start= demand
sc config InstallService start= demand
sc config uhssvc start= demand
sc config UsoSvc start= demand
sc config wuauserv start= demand
sc config LanmanServer start= demand
sc config NlaSvc start= demand
reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\DoSvc"" /v Start /t reg_dword /d 3 /f
reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\InstallService"" /v Start /t reg_dword /d 3 /f
reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\UsoSvc"" /v Start /t reg_dword /d 3 /f
reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\wuauserv"" /v Start /t reg_dword /d 3 /f
reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WaaSMedicSvc"" /v Start /t reg_dword /d 3 /f
reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\BITS"" /v Start /t reg_dword /d 3 /f
reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\upfc"" /v Start /t reg_dword /d 3 /f
reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\uhssvc"" /v Start /t reg_dword /d 3 /f
reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\ossrs"" /v Start /t reg_dword /d 3 /f
reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate"" /v ""DeferUpdatePeriod"" /t REG_DWORD /d ""0"" /f
reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate"" /v ""DeferUpgrade"" /t REG_DWORD /d ""0"" /f
reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate"" /v ""DeferUpgradePeriod"" /t REG_DWORD /d ""0"" /f
reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate"" /v ""DisableWindowsUpdateAccess"" /t REG_DWORD /d ""0"" /f
schtasks /Change /TN ""Microsoft\Windows\InstallService\ScanForUpdates"" /Enable
schtasks /Change /TN ""Microsoft\Windows\InstallService\ScanForUpdatesAsUser"" /Enable
schtasks /Change /TN ""Microsoft\Windows\InstallService\SmartRetry"" /Enable
schtasks /Change /TN ""Microsoft\Windows\InstallService\WakeUpAndContinueUpdates"" /Enable
schtasks /Change /TN ""Microsoft\Windows\InstallService\WakeUpAndScanForUpdates"" /Enable
schtasks /Change /TN ""Microsoft\Windows\UpdateOrchestrator\Report policies"" /Enable
schtasks /Change /TN ""Microsoft\Windows\UpdateOrchestrator\Schedule Scan"" /Enable
schtasks /Change /TN ""Microsoft\Windows\UpdateOrchestrator\Schedule Scan Static Task"" /Enable
schtasks /Change /TN ""Microsoft\Windows\UpdateOrchestrator\UpdateModelTask"" /Enable
schtasks /Change /TN ""Microsoft\Windows\UpdateOrchestrator\USO_UxBroker"" /Enable
schtasks /Change /TN ""Microsoft\Windows\WaaSMedic\PerformRemediation"" /Enable
schtasks /Change /TN ""Microsoft\Windows\WindowsUpdate\Scheduled Start"" /Enable
schtasks /Change /TN ""Microsoft\Windows\NlaSvc\WiFiTask"" /Enable
sc config RemoteRegistry start= demand
sc config RemoteAccess start= demand
sc config WinRM start= demand
sc config RmSvc start= demand
sc config PrintNotify start= demand
sc config Spooler start= demand
schtasks /Change /TN ""Microsoft\Windows\Printing\EduPrintProv"" /Enable
schtasks /Change /TN ""Microsoft\Windows\Printing\PrinterCleanupTask"" /Enable
sc config BTAGService start= demand
sc config bthserv start= demand
sc config LanmanWorkstation start= demand
sc config WdiServiceHost start= demand
sc config NcbService start= demand
sc config ndu start= demand
sc config Netman start= demand
sc config netprofm start= demand
sc config WwanSvc start= demand
sc config Dhcp start= auto
sc config DPS start= auto
sc config lmhosts start= auto
sc config NlaSvc start= auto
sc config nsi start= auto
sc config RmSvc start= auto
sc config Wcmsvc start= auto
sc config Winmgmt start= auto
sc config WlanSvc start= auto
schtasks /Change /TN ""Microsoft\Windows\WlanSvc\CDSSync"" /Enable
schtasks /Change /TN ""Microsoft\Windows\WCM\WiFiTask"" /Enable
schtasks /Change /TN ""Microsoft\Windows\NlaSvc\WiFiTask"" /Enable
schtasks /Change /TN ""Microsoft\Windows\DUSM\dusmtask"" /Enable
reg add ""HKLM\Software\Policies\Microsoft\Windows\NetworkConnectivityStatusIndicator"" /v ""NoActiveProbe"" /t REG_DWORD /d ""0"" /f
reg add ""HKLM\System\CurrentControlSet\Services\NlaSvc\Parameters\Internet"" /v ""EnableActiveProbing"" /t REG_DWORD /d ""1"" /f
reg add ""HKLM\System\CurrentControlSet\Services\BFE"" /v ""Start"" /t REG_DWORD /d ""2"" /f
reg add ""HKLM\System\CurrentControlSet\Services\Dnscache"" /v ""Start"" /t REG_DWORD /d ""2"" /f
reg add ""HKLM\System\CurrentControlSet\Services\WinHttpAutoProxySvc"" /v ""Start"" /t REG_DWORD /d ""3"" /f
net start DPS
net start nsi
net start NlaSvc
net start Dhcp
net start Wcmsvc
net start RmSvc
wmic path win32_networkadapter where index=0 call disable
wmic path win32_networkadapter where index=1 call disable
wmic path win32_networkadapter where index=2 call disable
wmic path win32_networkadapter where index=3 call disable
wmic path win32_networkadapter where index=4 call disable
wmic path win32_networkadapter where index=5 call disable
wmic path win32_networkadapter where index=0 call enable
wmic path win32_networkadapter where index=1 call enable
wmic path win32_networkadapter where index=2 call enable
wmic path win32_networkadapter where index=3 call enable
wmic path win32_networkadapter where index=4 call enable
wmic path win32_networkadapter where index=5 call enable
arp -d *
route -f
nbtstat -R
nbtstat -RR
netcfg -d
netsh winsock reset
netsh int 6to4 reset all
netsh int httpstunnel reset all
netsh int ip reset
netsh int isatap reset all
netsh int portproxy reset all
netsh int tcp reset all
netsh int teredo reset all
netsh branchcache reset
ipconfig /release
ipconfig /renew
        ";
            await Task.Run(() => BatchCommands(commands));
        }
        // Fix Tab end

        // Settings Tab
        private void translate_CheckedChanged(object sender, EventArgs e)
        {
            if (translate.Checked) {
                _ = SerbianTranslation();
            }
            else {
                _ = EnglishTranslation();
            }
        }

        private async Task EnglishTranslation()
        {
            // Sidebar
            TweaksButton.Text = "Tweaks";
            FixesButton.Text = "Fixes";
            SettingsButton.Text = "Settings";

            // Tweaks Tab
            label2.Text = "Windows Tweaks";
            label3.Text = "General Tweaks";
            label4.Text = "USB Tweaks";
            label5.Text = "Network Tweaks";
            label6.Text = "Debloat";
            label7.Text = "CPU";
            label8.Text = "Services";
            label15.Text = "Performance";
            label14.Text = "GPU";
            label13.Text = "Advanced";
            label12.Text = "Game Tweaks";

            // Fix Tab
            label29.Text = "Microsoft Store";
            label28.Text = "Microphone";
            label27.Text = "Activate Windows (free)";
            label26.Text = "Xbox Services";

            // Settings Tab
            label22.Text = "Auto Update";
            label21.Text = "Application Transparency";
            label20.Text = "Rounded App Corners";
            label19.Text = "Translate to Serbian";
        }

        private async Task SerbianTranslation()
        {
            // Sidebar
            TweaksButton.Text = "Tweakovi";
            FixesButton.Text = "Popravke";
            SettingsButton.Text = "Podesavanja";

            // Tweaks Tab
            label2.Text = "Windows Tweakovi";
            label3.Text = "Opsti Tweakovi";
            label4.Text = "USB Tweakovi";
            label5.Text = "Internet Tweakovi";
            label6.Text = "Ocisti";
            label7.Text = "Procesor";
            label8.Text = "Servisi";
            label15.Text = "Performanse";
            label14.Text = "Tweakovi za Graficku Karticu";
            label13.Text = "Napredno";
            label12.Text = "Podesavanja Igrica";

            // Fix Tab
            label29.Text = "Microsoft Prodavnica";
            label28.Text = "Mikrofon";
            label27.Text = "Aktiviraj Windows (besplatno)";
            label26.Text = "Xbox Servisi";

            // Settings Tab
            label22.Text = "Automatsko Azuriranje";
            label21.Text = "Providnost Aplikacije";
            label20.Text = "Zaobljeni Uglovi Aplikacije";
            label19.Text = "Prevedi Na Srpski";
        }

        private void roundedcorners_CheckedChanged(object sender, EventArgs e)
        {
            if (roundedcorners.Checked) {
                _ = RoundApp();
            }
            else {
                _ = SquareApp();
            }
        }

        private async Task RoundApp()
        {
            Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 20, 20));
        }

        private async Task SquareApp()
        {
            Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 0, 0));
        }

        private void apptransp_CheckedChanged(object sender, EventArgs e)
        {
            if (apptransp.Checked) {
                this.Opacity = 0.90;
            }
            else {
                this.Opacity = 1.0;
            }
        }

        private void updateapp_CheckedChanged(object sender, EventArgs e)
        {
            IsUpdateEnabled = updateapp.Checked;
        }

        private void desktopshortcu_Click(object sender, EventArgs e)
        {
            _ = CreateDesktopShortcut();
        }

        private async Task CreateDesktopShortcut()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            WshShell shell = new WshShell();

            string shortcutPath = Path.Combine(desktopPath, "Straya Free Tweaker v2.5.lnk");

            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
            shortcut.Description = "A Free Open Sourced Windows Tweaking Utility";
            shortcut.TargetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Straya Free Tweaker.exe");
            shortcut.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
            shortcut.Save();
        }
    }
}
