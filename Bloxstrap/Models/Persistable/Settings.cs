﻿using System.Collections.ObjectModel;
using System.Drawing;

namespace Bloxstrap.Models.Persistable
{
    public class Settings
    {
        // Will sort it out later
        // bloxstrap configuration
        public BootstrapperStyle BootstrapperStyle { get; set; } = BootstrapperStyle.FluentAeroDialog;
        public BootstrapperIcon BootstrapperIcon { get; set; } = BootstrapperIcon.IconBloxstrap;
        public string BootstrapperTitle { get; set; } = App.ProjectName;
        public string BootstrapperIconCustomLocation { get; set; } = "";
        public Theme Theme { get; set; } = Theme.Default;
        public CleanerOptions CleanerOptions { get; set; } = CleanerOptions.Never;
        public List<string> CleanerDirectories { get; set; } = new List<string>();
        public bool CheckForUpdates { get; set; } = true;
        public bool ConfirmLaunches { get; set; } = true;
        public string Locale { get; set; } = "nil";
        public bool ForceRobloxLanguage { get; set; } = false;
        public bool RobloxWiFiPriorityBoost { get; set; } = false;
        public bool UseFastFlagManager { get; set; } = true;
        public CopyFormatMode SelectedCopyFormat { get; set; } = CopyFormatMode.Format1; //change for default value
        public bool ShowPresetColumn { get; set; } = false; //change for default value
        public bool ShowFlagCount { get; set; } = false; // change for default value
        public bool ShowAddWithID { get; set; } = false; // change for default value
        public List<string> NavigationOrder { get; set; } = new List<string>();
        public bool IsNavigationOrderLocked { get; set; } = true;  // change for default value
        public bool WPFSoftwareRender { get; set; } = false;
        public bool DisableAnimations { get; set; } = false;
        public bool DisableHardwareAcceleration { get; set; } = false;
        public ProcessPriorityOption SelectedProcessPriority { get; set; } = ProcessPriorityOption.Normal;
        public string? CustomFontPath { get; set; } = null;
        public bool EnableAnalytics { get; set; } = false;
        public bool UpdateRoblox { get; set; } = true;
        public bool MultiInstanceLaunching { get; set; } = false;
        public string Channel { get; set; } = RobloxInterfaces.Deployment.DefaultChannel;
        public ChannelChangeMode ChannelChangeMode { get; set; } = ChannelChangeMode.Automatic;
        public string ChannelHash { get; set; } = "";
        public string DownloadingStringFormat { get; set; } = Strings.Bootstrapper_Status_Downloading + " {0} - {1}MB / {2}MB";
        public string? SelectedCustomTheme { get; set; } = null;

        // integration configuration
        public bool EnableActivityTracking { get; set; } = true;
        public bool UseDiscordRichPresence { get; set; } = true;
        public bool HideRPCButtons { get; set; } = true;
        public bool ShowAccountOnRichPresence { get; set; } = false;
        public bool ShowServerDetails { get; set; } = false;
        public ObservableCollection<CustomIntegration> CustomIntegrations { get; set; } = new();

        // mod preset configuration
        public bool UseDisableAppPatch { get; set; } = false;
        public bool BlockRobloxRecording { get; set; } = false;
        public bool BlockRobloxScreenshots { get; set; } = false;
        public RobloxIcon SelectedRobloxIcon { get; set; } = RobloxIcon.Default;
        public string? IconPath { get; set; } = null;
    }
}