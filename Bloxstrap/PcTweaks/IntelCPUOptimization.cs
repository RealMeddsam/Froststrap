using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using Microsoft.Win32;

namespace Bloxstrap.PcTweaks
{
    internal static class IntelCPUOptimization
    {
        public static bool ToggleIntelOptimizations(bool enable)
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
                    EnableIntelOptimizations();
                }
                else
                {
                    DisableIntelOptimizations();
                }

                Frontend.ShowMessageBox(
                    $"Intel CPU optimizations {(enable ? "enabled" : "disabled")} successfully.",
                    MessageBoxImage.Information,
                    MessageBoxButton.OK);

                return true;
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox(
                    $"Failed to {(enable ? "enable" : "disable")} Intel CPU optimizations:\n\n{ex.Message}",
                    MessageBoxImage.Error,
                    MessageBoxButton.OK);
                return false;
            }
        }

        private static void EnableIntelOptimizations()
        {
            RunCommand("powercfg -setacvalueindex scheme_current sub_processor CPMINCORES 100");
            RunCommand("powercfg /setactive SCHEME_CURRENT");
            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power", "CoreParkingDisabled", 1);

            RunCommand("powercfg -setacvalueindex scheme_current sub_sleep hybridsleep 0");

            RunCommand("powercfg -setacvalueindex scheme_current sub_sleep standbyidle 0");

            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power", "PlatformAoAcOverride", 0);

            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power", "CsEnabled", 0);

            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power", "EnergyEstimationEnabled", 0);

            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power\EnergyEstimation\TaggedEnergy", "DisableTaggedEnergyLogging", 1);
            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power\EnergyEstimation\TaggedEnergy", "TelemetryMaxApplication", 0);
            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power\EnergyEstimation\TaggedEnergy", "TelemetryMaxTagPerApplication", 0);
        }

        private static void DisableIntelOptimizations()
        {
            RunCommand("powercfg -setacvalueindex scheme_current sub_sleep hybridsleep 1");

            RunCommand("powercfg -setacvalueindex scheme_current sub_sleep standbyidle 1");

            RunCommand("powercfg -setacvalueindex scheme_current sub_processor CPMINCORES 10");
            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power", "CoreParkingDisabled", 0);
            RunCommand("powercfg /setactive SCHEME_CURRENT");

            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power", "PlatformAoAcOverride", 1);

            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power", "CsEnabled", 1);

            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power", "EnergyEstimationEnabled", 1);

            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power\EnergyEstimation\TaggedEnergy", "DisableTaggedEnergyLogging", 0);
            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power\EnergyEstimation\TaggedEnergy", "TelemetryMaxApplication", 1);
            SetRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power\EnergyEstimation\TaggedEnergy", "TelemetryMaxTagPerApplication", 1);
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

        public static bool IsIntelOptimizationEnabled()
        {
            try
            {
                int enabledCount = 0;
                int totalChecks = 0;

                if (CheckRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power", "CoreParkingDisabled", 1)) enabledCount++;
                totalChecks++;

                if (CheckRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power", "PlatformAoAcOverride", 0)) enabledCount++;
                totalChecks++;

                if (CheckRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power", "CsEnabled", 0)) enabledCount++;
                totalChecks++;

                if (CheckRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power", "EnergyEstimationEnabled", 0)) enabledCount++;
                totalChecks++;

                if (CheckRegistryDWORD(@"SYSTEM\CurrentControlSet\Control\Power\EnergyEstimation\TaggedEnergy", "DisableTaggedEnergyLogging", 1)) enabledCount++;
                totalChecks++;

                double enabledPercentage = (double)enabledCount / totalChecks;
                return enabledPercentage >= 0.7;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking Intel optimization state: {ex.Message}");
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