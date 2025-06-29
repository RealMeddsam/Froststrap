﻿using Bloxstrap.UI.ViewModels.Settings;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for BloxstrapPage.xaml
    /// </summary>
    public partial class BloxstrapPage
    {
        public BloxstrapPage()
        {
            DataContext = new BloxstrapViewModel();
            InitializeComponent();
        }

        private void OpenClickerGame() => new ClickerGame.MainWindow().ShowDialog();
        public ICommand OpenClickerGameCommand => new RelayCommand(OpenClickerGame);

        private const string EasterEggCode = "159753";

        private async void SubmitCode_Click(object sender, RoutedEventArgs e)
        {
            if (CodeInputBox.Text.Trim() == EasterEggCode)
            {
                var clickerGameWindow = new ClickerGame.MainWindow();
                clickerGameWindow.Owner = Window.GetWindow(this);
                clickerGameWindow.ShowDialog();
            }
            else
            {
                CodeInputBox.Text = "Incorrect code!";
                CodeInputBox.Foreground = System.Windows.Media.Brushes.Red;

                await Task.Delay(1000);

                if (CodeInputBox.Text == "Incorrect code!")
                {
                    CodeInputBox.Text = "";
                    CodeInputBox.ClearValue(ForegroundProperty);
                }
            }
        }

        private void CodeInputBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (CodeInputBox.Foreground == System.Windows.Media.Brushes.Red && CodeInputBox.Text != "Incorrect code!")
            {
                CodeInputBox.ClearValue(ForegroundProperty);
            }
            if (CodeInputBox.Text == "Incorrect code!")
            {
                CodeInputBox.ClearValue(ForegroundProperty);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var navService = NavigationService.GetNavigationService(this);
            navService?.Navigate(new PCTweaksPage());
        }

        private void CodeInputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SubmitCode_Click(sender, new RoutedEventArgs());
                e.Handled = true;
            }
        }
    }
}