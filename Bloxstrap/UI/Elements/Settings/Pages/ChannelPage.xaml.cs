using Bloxstrap.UI.ViewModels.Settings;
using Bloxstrap.UI.Elements.Dialogs;
using Microsoft.Win32;
using System.Windows;
using Wpf.Ui.Hardware;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for ChannelPage.xaml
    /// </summary>
    public partial class ChannelPage
    {
        public ChannelPage()
        {
            DataContext = new ChannelViewModel();
            InitializeComponent();
            (App.Current as App)?._froststrapRPC?.UpdatePresence("Page: Settings");
        }

        private void ResetSettings_Click(object sender, RoutedEventArgs e)
        {
            var confirm = Frontend.ShowMessageBox(
                "Are you sure you want to reset all settings to their default values?",
                MessageBoxImage.Warning,
                MessageBoxButton.YesNo
            );

            if (confirm != MessageBoxResult.Yes)
                return;

            string preservedUserId = App.Settings.Prop.UserId;

            App.Settings.Prop = new Models.Persistable.Settings();
            App.Settings.Prop.UserId = preservedUserId; // this is so the user dosent lose his id that he uses for publishing lists

            App.Settings.Save();

            Frontend.ShowMessageBox("Settings have been reset. Restarting the app...", MessageBoxImage.Information);

            System.Windows.Forms.Application.Restart();
            Application.Current.Shutdown();
        }

        private void ExportSettings_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                FileName = "FroststrapSettings.json"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                string source = Path.Combine(Paths.Base, "Settings.json");
                if (!File.Exists(source))
                {
                    Frontend.ShowMessageBox("No settings file found to export.", MessageBoxImage.Warning);
                    return;
                }

                File.Copy(source, dialog.FileName, overwrite: true);
                Frontend.ShowMessageBox($"Settings exported successfully to:\n{dialog.FileName}", MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox($"Failed to export settings: {ex.Message}", MessageBoxImage.Error);
            }
        }

        private void ImportSettings_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                string target = Path.Combine(Paths.Base, "Settings.json");

                File.Copy(dialog.FileName, target, overwrite: true);

                Frontend.ShowMessageBox("Settings imported successfully. Restarting the app...", MessageBoxImage.Information);
                System.Windows.Forms.Application.Restart();
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox($"Failed to import settings: {ex.Message}", MessageBoxImage.Error);
            }
        }

        private void OpenDebugMenu_Click(object sender, RoutedEventArgs e)
        {
            var debugMenu = new ContextMenu.DebugMenu();
            debugMenu.Show();
        }

        private void UninstallButton_Click(object sender, RoutedEventArgs e)
        {
            LaunchHandler.LaunchUninstaller();
        }

        private void ToggleSwitch_Checked_1(object sender, RoutedEventArgs e)
        {
            HardwareAcceleration.MemoryTrimming();
        }

        private void ToggleSwitch_Unchecked_1(object sender, RoutedEventArgs e)
        {
            Frontend.ShowMessageBox(
            Strings.Menu_Channels_HardwareAccelRestart,
            MessageBoxImage.Information
            );
        }

        private void ToggleSwitch_Checked_2(object sender, RoutedEventArgs e)
        {
            HardwareAcceleration.DisableAllAnimations();
        }

        private void ToggleSwitch_Unchecked_2(object sender, RoutedEventArgs e)
        {
            Frontend.ShowMessageBox(
            Strings.Menu_Channels_DisableAnimationRestart,
            MessageBoxImage.Information
            );
        }

        private void OpenChannelListDialog_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ChannelListsDialog();
            dialog.ShowDialog();
        }
    }
}