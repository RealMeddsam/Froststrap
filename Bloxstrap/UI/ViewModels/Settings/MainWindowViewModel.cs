﻿using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class MainWindowViewModel : NotifyPropertyChangedViewModel
    {
        public ICommand OpenAboutCommand => new RelayCommand(OpenAbout);
        public ICommand OpenHelpCommand => new RelayCommand(OpenHelp);

        public ICommand SaveSettingsCommand => new RelayCommand(SaveSettings);

        public ICommand SaveAndLaunchSettingsCommand => new RelayCommand(SaveAndLaunchSettings);


        public ICommand CloseWindowCommand => new RelayCommand(CloseWindow);

        public EventHandler? RequestSaveNoticeEvent;

        public EventHandler? RequestCloseWindowEvent;

        public bool TestModeEnabled
        {
            get => App.LaunchSettings.TestModeFlag.Active;
            set
            {
                if (value && !App.State.Prop.TestModeWarningShown)
                {
                    var result = Frontend.ShowMessageBox(Strings.Menu_TestMode_Prompt, MessageBoxImage.Information, MessageBoxButton.YesNo);

                    if (result != MessageBoxResult.Yes)
                        return;

                    App.State.Prop.TestModeWarningShown = true;
                }

                App.LaunchSettings.TestModeFlag.Active = value;
            }
        }

        private void OpenAbout() => new Elements.About.MainWindow().ShowDialog();
        private void OpenHelp() => new Elements.Help.MainWindow().ShowDialog();

        private void CloseWindow() => RequestCloseWindowEvent?.Invoke(this, EventArgs.Empty);

        private void SaveSettings()
        {
            const string LOG_IDENT = "MainWindowViewModel::SaveSettings";

            App.Settings.Save();
            App.State.Save();
            App.FastFlags.Save();

            foreach (var pair in App.PendingSettingTasks)
            {
                var task = pair.Value;

                if (task.Changed)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Executing pending task '{task}'");
                    task.Execute();
                }
            }

            App.PendingSettingTasks.Clear();

            RequestSaveNoticeEvent?.Invoke(this, EventArgs.Empty);
        }
        public void SaveAndLaunchSettings()
        {
            SaveSettings();
            if (!App.LaunchSettings.TestModeFlag.Active) // test mode already launches an instance
                LaunchHandler.LaunchRoblox(LaunchMode.Player);

            CloseWindow();
        }
    }
}
