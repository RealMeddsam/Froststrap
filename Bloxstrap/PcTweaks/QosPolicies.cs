using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;

namespace Bloxstrap.PcTweaks
{
    internal static class QosPolicies
    {
        private const string KeyPath = @"SOFTWARE\Policies\Microsoft\Windows\QoS\RobloxWiFiBoost";

        public static bool TogglePolicy(bool enable)
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
                    using var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(KeyPath);
                    key?.SetValue("ApplicationName", "RobloxPlayerBeta.exe", Microsoft.Win32.RegistryValueKind.String);
                    key?.SetValue("PolicyName", "RobloxNetworkBoost", Microsoft.Win32.RegistryValueKind.String);
                    key?.SetValue("Version", 1, Microsoft.Win32.RegistryValueKind.DWord);
                    key?.SetValue("DSCPValue", 46, Microsoft.Win32.RegistryValueKind.DWord);
                    key?.SetValue("ThrottleRate", unchecked((int)0xFFFFFFFF), Microsoft.Win32.RegistryValueKind.DWord);
                }
                else
                {
                    Microsoft.Win32.Registry.LocalMachine.DeleteSubKeyTree(KeyPath, throwOnMissingSubKey: false);
                }

                Frontend.ShowMessageBox(
                    "QoS policy updated. Please restart your PC for this to take full effect.",
                    MessageBoxImage.Information,
                    MessageBoxButton.OK);

                return true;
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox(
                    $"Failed to {(enable ? "enable" : "disable")} QoS policy:\n\n{ex.Message}",
                    MessageBoxImage.Error,
                    MessageBoxButton.OK);
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

        public static bool IsPolicyEnabled()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(KeyPath);
                if (key == null)
                    return false;

                var appName = key.GetValue("ApplicationName") as string;
                var policyName = key.GetValue("PolicyName") as string;
                var version = key.GetValue("Version");
                var dscp = key.GetValue("DSCPValue");

                return appName == "RobloxPlayerBeta.exe" &&
                       policyName == "RobloxNetworkBoost" &&
                       Convert.ToInt32(version) == 1 &&
                       Convert.ToInt32(dscp) == 46;
            }
            catch
            {
                return false;
            }
        }
    }
}