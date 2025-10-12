using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Bloxstrap.PcTweaks
{
    internal static class FirewallRules
    {
        private const string RuleName = "Froststrap - Roblox Firewall Access";

        public static bool ToggleFirewallRule(bool enable)
        {
            if (!IsRunningAsAdmin())
            {
                var result = Frontend.ShowMessageBox(
                    "This feature requires administrator privileges.\n\nRestart Froststrap as administrator?",
                    MessageBoxImage.Warning,
                    MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                    RestartElevated();

                return false;
            }

            try
            {
                if (enable)
                {
                    AddFirewallRule("in");
                    AddFirewallRule("out");
                }
                else
                {
                    RemoveFirewallRule("in");
                    RemoveFirewallRule("out");
                }

                Frontend.ShowMessageBox(
                    $"Firewall rules have been {(enable ? "added" : "removed")}.\n\n{(enable ? "Roblox will be allowed through the firewall." : "Restart Roblox to apply changes.")}",
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox(
                    $"Failed to {(enable ? "enable" : "remove")} firewall rule:\n\n{ex.Message}",
                    MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private static void AddFirewallRule(string direction)
        {
            var directionFlag = direction == "in" ? "in" : "out";
            var ruleName = $"{RuleName} ({directionFlag.ToUpper()})";

            Process.Start(new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = $"advfirewall firewall add rule name=\"{ruleName}\" dir={directionFlag} action=allow enable=yes profile=any",
                UseShellExecute = false,
                CreateNoWindow = true
            })?.WaitForExit();
        }

        private static void RemoveFirewallRule(string direction)
        {
            var directionFlag = direction == "in" ? "in" : "out";
            var ruleName = $"{RuleName} ({directionFlag.ToUpper()})";

            Process.Start(new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = $"advfirewall firewall delete rule name=\"{ruleName}\" dir={directionFlag}",
                UseShellExecute = false,
                CreateNoWindow = true
            })?.WaitForExit();
        }

        private static bool IsRunningAsAdmin()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void RestartElevated()
        {
            string? exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (exePath == null)
                return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = "-menu",
                    UseShellExecute = true,
                    Verb = "runas"
                });

                Application.Current.Shutdown();
            }
            catch { }
        }

        public static bool IsFirewallRuleEnabled()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "advfirewall firewall show rule name=all",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                };

                using Process proc = Process.Start(psi)!;
                if (proc == null)
                    return false;

                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                string pattern = @$"Rule Name:\s*{Regex.Escape(RuleName)} \((IN|OUT)\)[\s\S]*?Enabled:\s*Yes";

                var matches = Regex.Matches(output, pattern, RegexOptions.IgnoreCase);
                HashSet<string> directionsFound = new();

                foreach (Match match in matches)
                {
                    string direction = match.Groups[1].Value.ToLowerInvariant();
                    directionsFound.Add(direction);
                }

                return directionsFound.Contains("in") && directionsFound.Contains("out");
            }
            catch
            {
                return false;
            }
        }
    }
}