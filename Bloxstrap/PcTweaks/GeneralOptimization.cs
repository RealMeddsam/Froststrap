using System.Security.Principal;
using System.Windows;
using Microsoft.Win32;

namespace Bloxstrap.PcTweaks
{
    internal static class GeneralOptimization
    {
        public static bool ToggleOptimizations(bool enable)
        {
            if (!IsRunningAsAdmin())
            {
                var res = Frontend.ShowMessageBox(
                    "This feature requires administrator privileges.\n\nRestart Froststrap as administrator?",
                    MessageBoxImage.Warning,
                    MessageBoxButton.YesNo);

                if (res == MessageBoxResult.Yes)
                {
                    RestartElevated();
                }

                return false;
            }

            try
            {
                if (enable)
                {
                    EnableOptimizations();
                }
                else
                {
                    DisableOptimizations();
                }

                Frontend.ShowMessageBox(
                    $"General optimizations {(enable ? "enabled" : "disabled")} successfully.",
                    MessageBoxImage.Information,
                    MessageBoxButton.OK);

                return true;
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox(
                    $"Failed to {(enable ? "enable" : "disable")} general optimizations:\n\n{ex.Message}",
                    MessageBoxImage.Error,
                    MessageBoxButton.OK);
                return false;
            }
        }

        private static void EnableOptimizations()
        {
            RunCommand("bcdedit /set Disabledynamictick yes");
            RunCommand("bcdedit /deletevalue useplatformclock");
            RunCommand("bcdedit /set useplatformtick yes");

            RunCommand("bcdedit /set configaccesspolicy Default");
            RunCommand("bcdedit /set MSI Default");
            RunCommand("bcdedit /set usephysicaldestination No");
            RunCommand("bcdedit /set usefirmwarepcisettings No");

            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", 38);

            SetRegistryString(@"Control Panel\Desktop", "AutoEndTasks", "1");
            SetRegistryString(@"Control Panel\Desktop", "HungAppTimeout", "1000");
            SetRegistryString(@"Control Panel\Desktop", "WaitToKillAppTimeout", "1000");
            SetRegistryString(@"Control Panel\Desktop", "LowLevelHooksTimeout", "1000");
            SetRegistryString(@"Control Panel\Desktop", "MenuShowDelay", "0");
            SetRegistryString(@"SYSTEM\CurrentControlSet\Control", "WaitToKillServiceTimeout", "1000");

            SetRegistryDWORD(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Reliability", "TimeStampInterval", 1);
            SetRegistryDWORD(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Reliability", "IoPriority", 3);

            SetLatencyToleranceEnabled(true);

            SetRegistryDWORD(@"SOFTWARE\Policies\Microsoft\Biometrics", "Enabled", 0);

            SetRegistryDWORD(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 10);

            RunCommand("Auditpol /set /subcategory:\"Process Termination\" /failure:Disable /failure:Enable");
            RunCommand("Auditpol /set /subcategory:\"RPC Events\" /failure:Disable /failure:Enable");
            RunCommand("Auditpol /set /subcategory:\"Filtering Platform Connection\" /failure:Disable /failure:Enable");
            RunCommand("Auditpol /set /subcategory:\"DPAPI Activity\" /failure:Disable /failure:Disable");
            RunCommand("Auditpol /set /subcategory:\"IPsec Driver\" /success: /failure:Enable");
            RunCommand("Auditpol /set /subcategory:\"Other System Events\" /failure:Disable /failure:Enable");
            RunCommand("Auditpol /set /subcategory:\"Security State Change\" /failure:Disable /failure:Enable");
            RunCommand("Auditpol /set /subcategory:\"Security System Extension\" /failure:Disable /failure:Enable");
            RunCommand("Auditpol /set /subcategory:\"System Integrity\" /failure:Disable /failure:Enable");

            SetRegistryDWORD(@"System\CurrentControlSet\Control\WMI\Autologger\AutoLogger-Diagtrack-Listener", "Start", 0);
            SetRegistryDWORD(@"System\CurrentControlSet\Control\WMI\Autologger\DiagLog", "Start", 0);
            SetRegistryDWORD(@"System\CurrentControlSet\Control\WMI\Autologger\Diagtrack-Listener", "Start", 0);
            SetRegistryDWORD(@"System\CurrentControlSet\Control\WMI\Autologger\WiFiSession", "Start", 0);

            SetRegistryDWORD(@"SOFTWARE\Microsoft\PolicyManager\current\device\System", "AllowExperimentation", 0);
            SetRegistryDWORD(@"SOFTWARE\Microsoft\PolicyManager\default\System\AllowExperimentation", "value", 0);

            SetRegistryDWORD(@"SOFTWARE\Policies\Microsoft\Windows\Windows Feeds", "EnableFeeds", 0);
            SetRegistryDWORD(@"SOFTWARE\Policies\Microsoft", "AllowNewsAndInterests", 0);
            SetRegistryDWORD(@"SOFTWARE\Policies\Microsoft\Windows\System", "EnableActivityFeed", 0);
            SetRegistryDWORD(@"Control Panel\International\User Profile", "HttpAcceptLanguageOptOut", 1);
            SetRegistryDWORD(@"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 0);

            SetRegistryDWORD(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance", "MaintenanceDisabled", 1);

            SetNotificationsEnabled(false);

            SetRegistryDWORD(@"SOFTWARE\Microsoft\Windows\CurrentVersion\CDP", "CdpSessionUserAuthzPolicy", 0);
            SetRegistryDWORD(@"SOFTWARE\Microsoft\Windows\CurrentVersion\CDP", "NearShareChannelUserAuthzPolicy", 0);

            SetSyncEnabled(false);

            SetPreinstalledAppsEnabled(false);

            SetBackgroundAppsEnabled(false);

            SetRegistryDWORD(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 0);

            SetCompatibilityTweaksEnabled(true);

            SetTrackingEnabled(false);

            DeleteRegistryValue(@"SOFTWARE\Microsoft\Windows\Dwm", "OverlayTestMode");

            SetFSOEnabled(true);

            SetRegistryString(@"SOFTWARE\Microsoft\DirectX\UserGpuPreferences", "DirectXUserGlobalSettings", "VRROptimizeEnable=0;SwapEffectUpgradeEnable=1;");

            SetRegistryDWORD(@"SOFTWARE\Microsoft\Windows\CurrentVersion\DriverSearching", "SearchOrderConfig", 0);
            SetRegistryDWORD(@"SOFTWARE\Microsoft\WindowsUpdate\UpdatePolicy\PolicyState", "ExcludeWUDrivers", 1);
        }

        private static void DisableOptimizations()
        {
            RunCommand("bcdedit /set Disabledynamictick yes");
            RunCommand("bcdedit /deletevalue useplatformclock");
            RunCommand("bcdedit /set useplatformtick yes");

            ResetMMCSSPriorities();

            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", 10);

            SetRegistryString(@"Control Panel\Desktop", "AutoEndTasks", "1");
            SetRegistryString(@"Control Panel\Desktop", "HungAppTimeout", "1000");
            SetRegistryString(@"Control Panel\Desktop", "WaitToKillAppTimeout", "1000");
            SetRegistryString(@"Control Panel\Desktop", "LowLevelHooksTimeout", "1000");
            SetRegistryString(@"Control Panel\Desktop", "MenuShowDelay", "0");
            SetRegistryString(@"SYSTEM\CurrentControlSet\Control", "WaitToKillServiceTimeout", "1000");

            SetRegistryDWORD(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Reliability", "TimeStampInterval", 1);
            SetRegistryDWORD(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Reliability", "IoPriority", 1);

            SetLatencyToleranceEnabled(false);

            SetRegistryDWORD(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 14);

            SetRegistryDWORD(@"SOFTWARE\Policies\Microsoft\Biometrics", "Enabled", 1);

            SetRegistryDWORD(@"SOFTWARE\Microsoft\GameBar", "AllowAutoGameMode", 0);
            SetRegistryDWORD(@"SOFTWARE\Microsoft\GameBar", "AutoGameModeEnabled", 0);

            RunCommand("Auditpol /set /subcategory:\"Process Termination\" /failure:Disable /failure:Enable");
            RunCommand("Auditpol /set /subcategory:\"RPC Events\" /failure:Disable /failure:Enable");
            RunCommand("Auditpol /set /subcategory:\"Filtering Platform Connection\" /failure:Disable /failure:Enable");
            RunCommand("Auditpol /set /subcategory:\"DPAPI Activity\" /failure:Disable /failure:Disable");
            RunCommand("Auditpol /set /subcategory:\"IPsec Driver\" /success: /failure:Enable");
            RunCommand("Auditpol /set /subcategory:\"Other System Events\" /failure:Disable /failure:Enable");
            RunCommand("Auditpol /set /subcategory:\"Security State Change\" /failure:Disable /failure:Enable");
            RunCommand("Auditpol /set /subcategory:\"Security System Extension\" /failure:Disable /failure:Enable");
            RunCommand("Auditpol /set /subcategory:\"System Integrity\" /failure:Disable /failure:Enable");

            SetRegistryDWORD(@"System\CurrentControlSet\Control\WMI\Autologger\AutoLogger-Diagtrack-Listener", "Start", 1);
            SetRegistryDWORD(@"System\CurrentControlSet\Control\WMI\Autologger\DiagLog", "Start", 1);
            SetRegistryDWORD(@"System\CurrentControlSet\Control\WMI\Autologger\Diagtrack-Listener", "Start", 1);
            SetRegistryDWORD(@"System\CurrentControlSet\Control\WMI\Autologger\WiFiSession", "Start", 1);

            SetRegistryDWORD(@"SOFTWARE\Microsoft\PolicyManager\current\device\System", "AllowExperimentation", 1);
            SetRegistryDWORD(@"SOFTWARE\Microsoft\PolicyManager\default\System\AllowExperimentation", "value", 1);

            SetRegistryDWORD(@"SOFTWARE\Policies\Microsoft\Windows\Windows Feeds", "EnableFeeds", 1);
            SetRegistryDWORD(@"SOFTWARE\Policies\Microsoft", "AllowNewsAndInterests", 1);
            SetRegistryDWORD(@"SOFTWARE\Policies\Microsoft\Windows\System", "EnableActivityFeed", 1);
            SetRegistryDWORD(@"Control Panel\International\User Profile", "HttpAcceptLanguageOptOut", 1);
            SetRegistryDWORD(@"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 1);

            SetRegistryDWORD(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance", "MaintenanceDisabled", 0);

            SetNotificationsEnabled(true);

            SetRegistryDWORD(@"SOFTWARE\Microsoft\Windows\CurrentVersion\CDP", "CdpSessionUserAuthzPolicy", 1);
            SetRegistryDWORD(@"SOFTWARE\Microsoft\Windows\CurrentVersion\CDP", "NearShareChannelUserAuthzPolicy", 1);

            SetSyncEnabled(true);

            SetPreinstalledAppsEnabled(true);

            SetBackgroundAppsEnabled(true);

            SetRegistryDWORD(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 1);

            SetRegistryDWORD(@"SOFTWARE\Microsoft\GameBar", "AllowAutoGameMode", 1);
            SetRegistryDWORD(@"SOFTWARE\Microsoft\GameBar", "AutoGameModeEnabled", 1);

            SetCompatibilityTweaksEnabled(false);
        }

        private static void SetLatencyToleranceEnabled(bool enable)
        {
            string[] latencyKeys = {
                @"SYSTEM\CurrentControlSet\Services\DXGKrnl",
                @"SYSTEM\CurrentControlSet\Control\Power",
                @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Power"
            };

            foreach (string keyPath in latencyKeys)
            {
                if (enable)
                {
                    SetRegistryDWORD(keyPath, "MonitorLatencyTolerance", 1);
                    SetRegistryDWORD(keyPath, "MonitorRefreshLatencyTolerance", 1);
                    SetRegistryDWORD(keyPath, "ExitLatency", 1);
                    SetRegistryDWORD(keyPath, "ExitLatencyCheckEnabled", 1);
                    SetRegistryDWORD(keyPath, "Latency", 1);
                    SetRegistryDWORD(keyPath, "LatencyToleranceDefault", 1);
                    SetRegistryDWORD(keyPath, "LatencyToleranceFSVP", 1);
                    SetRegistryDWORD(keyPath, "LatencyTolerancePerfOverride", 1);
                    SetRegistryDWORD(keyPath, "LatencyToleranceScreenOffIR", 1);
                    SetRegistryDWORD(keyPath, "LatencyToleranceVSyncEnabled", 1);
                    SetRegistryDWORD(keyPath, "RtlCapabilityCheckLatency", 1);
                }
                else
                {
                    DeleteRegistryValue(keyPath, "MonitorLatencyTolerance");
                    DeleteRegistryValue(keyPath, "MonitorRefreshLatencyTolerance");
                    DeleteRegistryValue(keyPath, "ExitLatency");
                    DeleteRegistryValue(keyPath, "ExitLatencyCheckEnabled");
                    DeleteRegistryValue(keyPath, "Latency");
                    DeleteRegistryValue(keyPath, "LatencyToleranceDefault");
                    DeleteRegistryValue(keyPath, "LatencyToleranceFSVP");
                    DeleteRegistryValue(keyPath, "LatencyTolerancePerfOverride");
                    DeleteRegistryValue(keyPath, "LatencyToleranceScreenOffIR");
                    DeleteRegistryValue(keyPath, "LatencyToleranceVSyncEnabled");
                    DeleteRegistryValue(keyPath, "RtlCapabilityCheckLatency");
                }
            }
        }

        private static void ResetMMCSSPriorities()
        {
            string[] mmcssTasks = { "Low Latency", "Games" };
            string[] mmcssValues = { "Affinity", "Background Only", "BackgroundPriority", "Clock Rate", "GPU Priority", "Priority", "Scheduling Category", "SFIO Priority", "Latency Sensitive" };

            foreach (string task in mmcssTasks)
            {
                foreach (string value in mmcssValues)
                {
                    DeleteRegistryValue($@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\{task}", value);
                }
            }
        }

        private static void SetNotificationsEnabled(bool enable)
        {
            int value = enable ? 1 : 0;
            SetRegistryDWORD(@"SOFTWARE\Microsoft\Windows\CurrentVersion\PushNotifications", "ToastEnabled", value);
            SetRegistryDWORD(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings", "NOC_GLOBAL_SETTING_ALLOW_NOTIFICATION_SOUND", value);
            SetRegistryDWORD(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings", "NOC_GLOBAL_SETTING_ALLOW_CRITICAL_TOASTS_ABOVE_LOCK", value);
            SetRegistryDWORD(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\QuietHours", "Enabled", value);
            SetRegistryDWORD(@"SOFTWARE\Policies\Microsoft\Windows\Explorer", "DisableNotificationCenter", enable ? 0 : 1);
        }

        private static void SetSyncEnabled(bool enable)
        {
            int value = enable ? 1 : 0;
            int policyValue = enable ? 0 : 2;

            SetRegistryDWORD(@"SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync", "SyncPolicy", 5);

            string[] syncGroups = { "Accessibility", "AppSync", "BrowserSettings", "Credentials", "DesktopTheme", "Language", "PackageState", "Personalization", "StartLayout", "Windows" };
            foreach (string group in syncGroups)
            {
                SetRegistryDWORD($@"SOFTWARE\Microsoft\Windows\CurrentVersion\SettingSync\Groups\{group}", "Enabled", value);
            }

            SetRegistryDWORD(@"SOFTWARE\Policies\Microsoft\Windows\SettingSync", "DisableSettingSync", policyValue);
            SetRegistryDWORD(@"SOFTWARE\Policies\Microsoft\Windows\SettingSync", "DisableSettingSyncUserOverride", enable ? 0 : 1);
        }

        private static void SetPreinstalledAppsEnabled(bool enable)
        {
            int value = enable ? 1 : 0;
            SetRegistryDWORD(@"SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "PreInstalledAppsEnabled", value);
            SetRegistryDWORD(@"SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SilentInstalledAppsEnabled", value);
            SetRegistryDWORD(@"SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "OemPreInstalledAppsEnabled", value);
            SetRegistryDWORD(@"SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "ContentDeliveryAllowed", value);
            SetRegistryDWORD(@"SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContentEnabled", value);
            SetRegistryDWORD(@"SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "PreInstalledAppsEverEnabled", value);
        }

        private static void SetBackgroundAppsEnabled(bool enable)
        {
            int value = enable ? 0 : 1;
            int serviceStart = enable ? 2 : 4;

            SetRegistryDWORD(@"SOFTWARE\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled", value);
            SetRegistryDWORD(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Search", "BackgroundAppGlobalToggle", enable ? 1 : 0);
            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Services\bam", "Start", serviceStart);
            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Services\dam", "Start", serviceStart);
        }

        private static void SetCompatibilityTweaksEnabled(bool enable)
        {
            int value = enable ? 0 : 1;
            int disableValue = enable ? 1 : 0;

            SetRegistryDWORD(@"Software\Policies\Microsoft\Windows\AppCompat", "AITEnable", value);
            SetRegistryDWORD(@"Software\Policies\Microsoft\Windows\AppCompat", "AllowTelemetry", value);
            SetRegistryDWORD(@"Software\Policies\Microsoft\Windows\AppCompat", "DisableInventory", disableValue);
            SetRegistryDWORD(@"Software\Policies\Microsoft\Windows\AppCompat", "DisableUAR", disableValue);
            SetRegistryDWORD(@"Software\Policies\Microsoft\Windows\AppCompat", "DisableEngine", disableValue);
            SetRegistryDWORD(@"Software\Policies\Microsoft\Windows\AppCompat", "DisablePCA", disableValue);
        }

        private static void SetTrackingEnabled(bool enable)
        {
            int value = enable ? 1 : 0;

            SetRegistryDWORD(@"Software\Microsoft\Windows\CurrentVersion\Privacy", "TailoredExperiencesWithDiagnosticDataEnabled", value);
            SetRegistryDWORD(@"Software\Microsoft\Windows\CurrentVersion\Diagnostics\DiagTrack", "ShowedToastAtLevel", 1);
            SetRegistryDWORD(@"Software\Microsoft\Input\TIPC", "Enabled", value);
            SetRegistryDWORD(@"Software\Policies\Microsoft\Windows\System", "UploadUserActivities", value);
            SetRegistryDWORD(@"Software\Policies\Microsoft\Windows\System", "PublishUserActivities", value);
            SetRegistryDWORD(@"Control Panel\International\User Profile", "HttpAcceptLanguageOptOut", 1);
            SetRegistryDWORD(@"Software\Microsoft\Windows\CurrentVersion\Policies\Attachments", "SaveZoneInformation", 1);
            SetRegistryDWORD(@"System\CurrentControlSet\Control\Diagnostics\Performance", "DisablediagnosticTracing", 1);
            SetRegistryDWORD(@"Software\Policies\Microsoft\Windows\WDI\{9c5a40da-b965-4fc3-8781-88dd50a6299d}", "ScenarioExecutionEnabled", value);

            if (enable)
            {
                RunCommand("schtasks /change /tn \"\\Microsoft\\Windows\\Application Experience\\StartupAppTask\" /Enable");
                RunCommand("schtasks /change /tn \"\\Microsoft\\Windows\\DiskDiagnostic\\Microsoft-Windows-DiskDiagnosticDataCollector\" /Enable");
                RunCommand("schtasks /change /tn \"\\Microsoft\\Windows\\DiskDiagnostic\\Microsoft-Windows-DiskDiagnosticResolver\" /Enable");
                RunCommand("schtasks /change /tn \"\\Microsoft\\Windows\\Power Efficiency Diagnostics\\AnalyzeSystem\" /Enable");
            }
            else
            {
                RunCommand("schtasks /change /tn \"\\Microsoft\\Windows\\Application Experience\\StartupAppTask\" /Disable");
                RunCommand("schtasks /end /tn \"\\Microsoft\\Windows\\DiskDiagnostic\\Microsoft-Windows-DiskDiagnosticDataCollector\"");
                RunCommand("schtasks /change /tn \"\\Microsoft\\Windows\\DiskDiagnostic\\Microsoft-Windows-DiskDiagnosticDataCollector\" /Disable");
                RunCommand("schtasks /end /tn \"\\Microsoft\\Windows\\DiskDiagnostic\\Microsoft-Windows-DiskDiagnosticResolver\"");
                RunCommand("schtasks /change /tn \"\\Microsoft\\Windows\\DiskDiagnostic\\Microsoft-Windows-DiskDiagnosticResolver\" /Disable");
                RunCommand("schtasks /end /tn \"\\Microsoft\\Windows\\Power Efficiency Diagnostics\\AnalyzeSystem\"");
                RunCommand("schtasks /change /tn \"\\Microsoft\\Windows\\Power Efficiency Diagnostics\\AnalyzeSystem\" /Disable");
            }
        }

        private static void SetFSOEnabled(bool enable)
        {
            int value = enable ? 0 : 1;
            SetRegistryDWORD(@"SYSTEM\GameConfigStore", "GameDVR_DSEBehavior", value);
            SetRegistryDWORD(@"SYSTEM\GameConfigStore", "GameDVR_FSEBehaviorMode", value);
            SetRegistryDWORD(@"SYSTEM\GameConfigStore", "GameDVR_EFSEFeatureFlags", value);
            SetRegistryDWORD(@"SYSTEM\GameConfigStore", "GameDVR_DXGIHonorFSEWindowsCompatible", value);
            SetRegistryDWORD(@"SYSTEM\GameConfigStore", "GameDVR_HonorUserFSEBehaviorMode", value);
        }

        private static void SetRegistryDWORD(string keyPath, string valueName, int value)
        {
            try
            {
                using var key = Registry.LocalMachine.CreateSubKey(keyPath);
                key?.SetValue(valueName, value, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to set {keyPath}\\{valueName}: {ex.Message}");
            }
        }

        private static void SetRegistryString(string keyPath, string valueName, string value)
        {
            try
            {
                using var key = Registry.LocalMachine.CreateSubKey(keyPath);
                key?.SetValue(valueName, value, RegistryValueKind.String);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to set {keyPath}\\{valueName}: {ex.Message}");
            }
        }

        private static void DeleteRegistryValue(string keyPath, string valueName)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(keyPath, true);
                key?.DeleteValue(valueName, false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to delete {keyPath}\\{valueName}: {ex.Message}");
            }
        }

        private static void RunCommand(string command)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/C {command}",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit(5000);

                if (process.ExitCode != 0)
                {
                    Debug.WriteLine($"Command failed: {command}");
                    Debug.WriteLine($"Error: {error}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to run command '{command}': {ex.Message}");
            }
        }

        public static bool IsOptimizationEnabled()
        {
            try
            {
                int enabledCount = 0;
                int totalChecks = 0;

                if (CheckBCDSetting("disabledynamictick", "yes")) enabledCount++;
                totalChecks++;

                if (CheckRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", 38)) enabledCount++;
                totalChecks++;

                if (CheckRegistryDWORD(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 10)) enabledCount++;
                totalChecks++;

                if (CheckRegistryDWORD(@"SOFTWARE\Policies\Microsoft\Biometrics", "Enabled", 0)) enabledCount++;
                totalChecks++;

                if (CheckRegistryDWORD(@"SOFTWARE\Policies\Microsoft\Windows\Windows Feeds", "EnableFeeds", 0)) enabledCount++;
                totalChecks++;

                if (CheckRegistryDWORD(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance", "MaintenanceDisabled", 1)) enabledCount++;
                totalChecks++;

                if (CheckRegistryDWORD(@"SOFTWARE\Microsoft\Windows\CurrentVersion\PushNotifications", "ToastEnabled", 0)) enabledCount++;
                totalChecks++;

                if (CheckRegistryDWORD(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 0)) enabledCount++;
                totalChecks++;

                double enabledPercentage = (double)enabledCount / totalChecks;
                return enabledPercentage >= 0.7;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking optimization state: {ex.Message}");
                return false;
            }
        }

        private static bool CheckBCDSetting(string setting, string expectedValue)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "bcdedit",
                        Arguments = $"/enum",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        StandardOutputEncoding = System.Text.Encoding.UTF8
                    }
                };
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(3000);

                if (process.ExitCode != 0)
                    return false;

                string[] lines = output.Split('\n');
                foreach (string line in lines)
                {
                    if (line.Trim().StartsWith(setting, StringComparison.OrdinalIgnoreCase))
                    {
                        return line.ToLower().Contains(expectedValue.ToLower());
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool CheckRegistryDWORD(string keyPath, string valueName, int expectedValue)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(keyPath);
                if (key == null)
                    return false;

                var value = key.GetValue(valueName);
                return value != null && Convert.ToInt32(value) == expectedValue;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsRunningAsAdmin()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void RestartElevated()
        {
            string? exeName = Process.GetCurrentProcess().MainModule?.FileName;
            if (exeName == null)
                return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exeName,
                    Arguments = "-menu",
                    UseShellExecute = true,
                    Verb = "runas"
                });

                Application.Current.Shutdown();
            }
            catch { }
        }
    }
}