﻿using System.Reflection;
using System.Windows;
using System.Windows.Input;
using Bloxstrap.Resources;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Wpf.Ui.Mvvm.Interfaces;

namespace Bloxstrap.UI.Elements.Dialogs
{
    /// <summary>
    /// Interaction logic for AdvancedSettingsDialog.xaml
    /// </summary>
    public partial class AdvancedSettingsDialog
    {
        public AdvancedSettingViewModel ViewModel { get; } = new();
        public static AdvancedSettingViewModel SharedViewModel { get; private set; } = new();

        public event EventHandler? SettingsSaved;

        public AdvancedSettingsDialog()
        {
            InitializeComponent();
            ViewModel.LoadSettings();
            DataContext = ViewModel;
            SharedViewModel = ViewModel;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SaveSettings();
            SettingsSaved?.Invoke(this, EventArgs.Empty);
        }
    }
}
