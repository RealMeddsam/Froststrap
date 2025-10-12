using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using Microsoft.Win32;

namespace Bloxstrap.PcTweaks
{
    internal static class DisablePowerSavingFeatures
    {
        public static bool TogglePowerSavingFeatures(bool enable)
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
                    DisablePowerSaving();
                }
                else
                {
                    EnablePowerSaving();
                }

                Frontend.ShowMessageBox(
                    $"Power saving features {(enable ? "disabled" : "enabled")} successfully.",
                    MessageBoxImage.Information,
                    MessageBoxButton.OK);

                return true;
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox(
                    $"Failed to {(enable ? "disable" : "enable")} power saving features:\n\n{ex.Message}",
                    MessageBoxImage.Error,
                    MessageBoxButton.OK);
                return false;
            }
        }

        private static void DisablePowerSaving()
        {
            DisableStorPortIdle();

            DisableStoragePowerFeatures();

            DisableCoalescingTimers();

            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", 1);

            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", 0);

            RunCommand("powercfg /h off");
            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power", "HibernateEnabled", 0);
            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power", "SleepReliabilityDetailedDiagnostics", 0);

            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "SleepStudyDisabled", 1);

            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power", "PlatformAoAcOverride", 0);
            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power", "EventProcessorEnabled", 0);
            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power", "CsEnabled", 0);
        }

        private static void EnablePowerSaving()
        {
            EnableStoragePowerFeatures();

            EnableCoalescingTimers();

            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", 0);

            RunCommand("powercfg –restoredefaultschemes");

            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", 1);

            RunCommand("powercfg /h on");
            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power", "HibernateEnabled", 1);
            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power", "SleepReliabilityDetailedDiagnostics", 1);

            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "SleepStudyDisabled", 0);

            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power", "PlatformAoAcOverride", 1);
            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power", "EventProcessorEnabled", 1);
            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power", "CsEnabled", 1);
        }

        private static void DisableStorPortIdle()
        {
            try
            {
                using var enumKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum");
                if (enumKey != null)
                {
                    foreach (string subKeyName in enumKey.GetSubKeyNames())
                    {
                        try
                        {
                            using var deviceKey = enumKey.OpenSubKey(subKeyName, true);
                            if (deviceKey != null && deviceKey.GetSubKeyNames().Contains("StorPort"))
                            {
                                using var storPortKey = deviceKey.OpenSubKey("StorPort", true);
                                if (storPortKey != null)
                                {
                                    storPortKey.SetValue("EnableIdlePowerManagement", 0, RegistryValueKind.DWord);
                                }
                            }
                        }
                        catch
                        {
                            // Continue if we can't access a specific device
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to disable StorPort idle: {ex.Message}");
            }
        }

        private static void DisableStoragePowerFeatures()
        {
            string[] powerFeatures = { "EnableHIPM", "EnableDIPM", "EnableHDDParking" };

            foreach (string feature in powerFeatures)
            {
                try
                {
                    using var servicesKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services");
                    if (servicesKey != null)
                    {
                        DisablePowerFeatureRecursive(servicesKey, feature);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to disable {feature}: {ex.Message}");
                }
            }
        }

        private static void EnableStoragePowerFeatures()
        {
            string[] powerFeatures = { "EnableHIPM", "EnableDIPM", "EnableHDDParking" };

            foreach (string feature in powerFeatures)
            {
                try
                {
                    using var servicesKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services");
                    if (servicesKey != null)
                    {
                        EnablePowerFeatureRecursive(servicesKey, feature);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to enable {feature}: {ex.Message}");
                }
            }
        }

        private static void DisablePowerFeatureRecursive(RegistryKey key, string featureName)
        {
            try
            {
                if (key.GetValue(featureName) != null)
                {
                    key.SetValue(featureName, 0, RegistryValueKind.DWord);
                }

                // Recursively check subkeys
                foreach (string subKeyName in key.GetSubKeyNames())
                {
                    try
                    {
                        using var subKey = key.OpenSubKey(subKeyName, true);
                        if (subKey != null)
                        {
                            DisablePowerFeatureRecursive(subKey, featureName);
                        }
                    }
                    catch
                    {
                        // Continue if we can't access a subkey
                    }
                }
            }
            catch
            {
                // Ignore errors in recursive search
            }
        }

        private static void EnablePowerFeatureRecursive(RegistryKey key, string featureName)
        {
            try
            {
                if (key.GetValue(featureName) != null)
                {
                    key.SetValue(featureName, 1, RegistryValueKind.DWord);
                }

                foreach (string subKeyName in key.GetSubKeyNames())
                {
                    try
                    {
                        using var subKey = key.OpenSubKey(subKeyName, true);
                        if (subKey != null)
                        {
                            EnablePowerFeatureRecursive(subKey, featureName);
                        }
                    }
                    catch
                    {
                        // Continue if we can't access a subkey
                    }
                }
            }
            catch
            {
                // Ignore errors in recursive search
            }
        }

        private static void DisableCoalescingTimers()
        {
            string[] coalescingPaths = {
                @"SYSTEM\CurrentControlSet\Control\Session Manager",
                @"SYSTEM\CurrentControlSet\Control\Session Manager\Power",
                @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
                @"SYSTEM\CurrentControlSet\Control\Session Manager\kernel",
                @"SYSTEM\CurrentControlSet\Control\Session Manager\Executive",
                @"SYSTEM\CurrentControlSet\Control\Power\ModernSleep",
                @"SYSTEM\CurrentControlSet\Control\Power"
            };

            foreach (string path in coalescingPaths)
            {
                SetRegistryDWORD(path, "CoalescingTimerInterval", 0);
            }
        }

        private static void EnableCoalescingTimers()
        {
            string[] coalescingPaths = {
                @"SYSTEM\CurrentControlSet\Control\Session Manager",
                @"SYSTEM\CurrentControlSet\Control\Session Manager\Power",
                @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
                @"SYSTEM\CurrentControlSet\Control\Session Manager\kernel",
                @"SYSTEM\CurrentControlSet\Control\Session Manager\Executive",
                @"SYSTEM\CurrentControlSet\Control\Power\ModernSleep",
                @"SYSTEM\CurrentControlSet\Control\Power"
            };

            foreach (string path in coalescingPaths)
            {
                SetRegistryDWORD(path, "CoalescingTimerInterval", 1);
            }
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

        public static bool IsPowerSavingDisabled()
        {
            try
            {
                int disabledCount = 0;
                int totalChecks = 0;

                // Check key power saving settings
                if (CheckRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", 1)) disabledCount++;
                totalChecks++;

                if (CheckRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", 0)) disabledCount++;
                totalChecks++;

                if (CheckRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power", "HibernateEnabled", 0)) disabledCount++;
                totalChecks++;

                if (CheckRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "SleepStudyDisabled", 1)) disabledCount++;
                totalChecks++;

                // Consider power saving disabled if majority of key settings are configured
                double disabledPercentage = (double)disabledCount / totalChecks;
                return disabledPercentage >= 0.7;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking power saving state: {ex.Message}");
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