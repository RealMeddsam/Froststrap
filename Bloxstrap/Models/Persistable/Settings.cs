using System.Collections.ObjectModel;
using System.Windows;

namespace Bloxstrap.Models.Persistable
{
    public class Settings
    {
        // Integration Page
        public bool EnableActivityTracking { get; set; } = true;
        public bool ShowServerDetails { get; set; } = true;
        public bool ShowGameHistoryMenu { get; set; } = true;
        public bool PlaytimeCounter { get; set; } = true;
        public TrayDoubleClickAction DoubleClickAction { get; set; } = TrayDoubleClickAction.ServerInfo;
        public bool UseDisableAppPatch { get; set; } = false;
        public bool BlockRobloxRecording { get; set; } = false;
        public bool BlockRobloxScreenshots { get; set; } = false;
        public bool ShowUsingFroststrapRPC { get; set; } = true;
        public bool UseDiscordRichPresence { get; set; } = true;
        public bool HideRPCButtons { get; set; } = true;
        public bool EnableCustomStatusDisplay { get; set; } = true;
        public bool ShowAccountOnRichPresence { get; set; } = false;
        public ObservableCollection<CustomIntegration> CustomIntegrations { get; set; } = new();

        // Bootstrapper Page
        public bool ConfirmLaunches { get; set; } = true;
        public bool AutoCloseCrashHandler { get; set; } = false;
        public string Locale { get; set; } = "nil";
        public bool ForceRobloxLanguage { get; set; } = false;
        public CleanerOptions CleanerOptions { get; set; } = CleanerOptions.Never;
        public List<string> CleanerDirectories { get; set; } = new List<string>();
        public bool BackgroundUpdatesEnabled { get; set; } = false;
        public bool MultiInstanceLaunching { get; set; } = false;
        public RobloxIcon SelectedRobloxIcon { get; set; } = RobloxIcon.Default;
        public ProcessPriorityOption SelectedProcessPriority { get; set; } = ProcessPriorityOption.Normal;

        // Mods Page
        public string ShiftlockCursorSelectedPath { get; set; } = "";
        public string ArrowCursorSelectedPath { get; set; } = "";
        public string ArrowFarCursorSelectedPath { get; set; } = "";
        public string IBeamCursorSelectedPath { get; set; } = "";

        // FastFlag Editor/Settings Related
        public bool UseFastFlagManager { get; set; } = true;
        public CopyFormatMode SelectedCopyFormat { get; set; } = CopyFormatMode.Format1;
        public bool CtrlCJsonFormat { get; set; } = false;
        public bool ShowPresetColumn { get; set; } = false;
        public bool ShowFlagCount { get; set; } = true;
        public bool ShowAddWithID { get; set; } = false;
        public bool AutomaticallyRemovedAltManually { get; set; } = true;

        // Appearance Page
        public BootstrapperStyle BootstrapperStyle { get; set; } = BootstrapperStyle.CustomFluentDialog;
        public BootstrapperIcon BootstrapperIcon { get; set; } = BootstrapperIcon.IconBloxstrap;
        public UIBackgroundType SelectedBackdrop { get; set; } = UIBackgroundType.Mica;
        public string? SelectedCustomTheme { get; set; } = null;
        public string BootstrapperTitle { get; set; } = App.ProjectName;
        public string BootstrapperIconCustomLocation { get; set; } = "";
        public string DownloadingStringFormat { get; set; } = Strings.Bootstrapper_Status_Downloading + " {0} - {1}MB / {2}MB";
        public Theme Theme { get; set; } = Theme.Default;
        public List<GradientStopData> CustomGradientStops { get; set; } = new()
        {
            new GradientStopData { Offset = 0.0, Color = "#4D5560" },
            new GradientStopData { Offset = 0.5, Color = "#383F47" },
            new GradientStopData { Offset = 1.0, Color = "#252A30" }
        };
        public Point GradientStartPoint { get; set; } = new Point(1, 1);
        public Point GradientEndPoint { get; set; } = new Point(0, 0);
        public string ImageBackgroundPath { get; set; } = string.Empty;
        public CustomBackgroundMode BackgroundMode { get; set; } = CustomBackgroundMode.Gradient;
        public BackgroundImageStretchMode BackgroundImageStretch { get; set; } = BackgroundImageStretchMode.Fill;
        public double BlackOverlayOpacity { get; set; } = 0.2;
        public string? CustomFontPath { get; set; } = null;

        // No pc tweaks since it checks registry values directly

        // Shortcuts Page
        public string GameShortcutsJson { get; set; } = "[]";

        // Community Page
        public string UserId { get; set; } = string.Empty;

        // Settings Page
        public bool CheckForUpdates { get; set; } = true;
        public bool WPFSoftwareRender { get; set; } = false;
        public bool DisableAnimations { get; set; } = false;
        public bool UpdateRoblox { get; set; } = true;
        public string Channel { get; set; } = RobloxInterfaces.Deployment.DefaultChannel;
        public ChannelChangeMode ChannelChangeMode { get; set; } = ChannelChangeMode.Ignore;
        public string ChannelHash { get; set; } = "";

        // Misc Stuff
        public bool IsNavigationSidebarExpanded { get; set; } = true;
        public bool EnableAnalytics { get; set; } = false;
        public bool DebugDisableVersionPackageCleanup { get; set; } = false;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool DeveloperMode { get; set; } = false;
        public WebEnvironment WebEnvironment { get; set; } = WebEnvironment.Production;

        // Clicker Game Configuration
        public string Points { get; set; } = "0";
        public string PointsPerClick { get; set; } = "1";
        public bool AutoClickerEnabled { get; set; } = false;
        public decimal BonusMultiplier { get; set; } = 1.0m;
        public int BonusMultiplierLevel { get; set; } = 0;
        public int CriticalClickChancePercent { get; set; } = 0;
        public int CriticalClickMultiplier { get; set; } = 2;
        public int UpgradeDiscountPercent { get; set; } = 0;
        public string TotalPointsSpent { get; set; } = "0";
        public string TotalPointsEarned { get; set; } = "0";
        public long TotalPlaytimeTicks { get; set; } = 0;

        // Clicker Game Prices
        public string DoubleClickPowerPrice { get; set; } = "50";
        public string AutoClickerPrice { get; set; } = "50";
        public string BonusMultiplierPrice { get; set; } = "250";
        public string CriticalClickChancePrice { get; set; } = "2000";
        public string CriticalClickMultiplierPrice { get; set; } = "4000";
        public string UpgradeDiscountPrice { get; set; } = "3000";
    }
}