using Bloxstrap.PcTweaks;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public partial class PCTweaksViewModel : NotifyPropertyChangedViewModel
    {
        private bool _isSettingRobloxWiFiPriorityBoost = false;
        private bool _isSettingAllowRobloxFirewall = false;
        private bool _isSettingGeneralOptimization = false;
        private bool _isSettingDisablePowerSavingFeatures = false;
        private bool _isSettingIntelOptimizations = false;
        private bool _isSettingAmdOptimizations = false;

        public PCTweaksViewModel()
        {
            RefreshAllProperties();
        }

        public bool RobloxWiFiPriorityBoost
        {
            get => QosPolicies.IsPolicyEnabled();
            set
            {
                if (_isSettingRobloxWiFiPriorityBoost || QosPolicies.IsPolicyEnabled() == value)
                    return;

                _isSettingRobloxWiFiPriorityBoost = true;

                try
                {
                    bool success = QosPolicies.TogglePolicy(value);
                    if (success)
                    {
                        OnPropertyChanged(nameof(RobloxWiFiPriorityBoost));
                    }
                    else
                    {
                        OnPropertyChanged(nameof(RobloxWiFiPriorityBoost));
                    }
                }
                finally
                {
                    _isSettingRobloxWiFiPriorityBoost = false;
                }
            }
        }

        public bool AllowRobloxFirewall
        {
            get => FirewallRules.IsFirewallRuleEnabled();
            set
            {
                if (_isSettingAllowRobloxFirewall || FirewallRules.IsFirewallRuleEnabled() == value)
                    return;

                _isSettingAllowRobloxFirewall = true;

                try
                {
                    bool success = FirewallRules.ToggleFirewallRule(value);
                    if (success)
                    {
                        OnPropertyChanged(nameof(AllowRobloxFirewall));
                    }
                    else
                    {
                        OnPropertyChanged(nameof(AllowRobloxFirewall));
                    }
                }
                finally
                {
                    _isSettingAllowRobloxFirewall = false;
                }
            }
        }

        public bool GeneralOptimizationsEnabled
        {
            get => GeneralOptimization.IsOptimizationEnabled();
            set
            {
                if (_isSettingGeneralOptimization || GeneralOptimization.IsOptimizationEnabled() == value)
                    return;

                _isSettingGeneralOptimization = true;

                try
                {
                    bool success = GeneralOptimization.ToggleOptimizations(value);
                    if (success)
                    {
                        OnPropertyChanged(nameof(GeneralOptimizationsEnabled));
                        OnPropertyChanged(nameof(RobloxWiFiPriorityBoost));
                        OnPropertyChanged(nameof(AllowRobloxFirewall));
                    }
                    else
                    {
                        // If failed, revert the UI
                        OnPropertyChanged(nameof(GeneralOptimizationsEnabled));
                    }
                }
                finally
                {
                    _isSettingGeneralOptimization = false;
                }
            }
        }

        public bool DisablePowerSavingFeature
        {
            get => DisablePowerSavingFeatures.IsPowerSavingDisabled();
            set
            {
                if (_isSettingDisablePowerSavingFeatures || DisablePowerSavingFeatures.IsPowerSavingDisabled() == value)
                    return;

                _isSettingDisablePowerSavingFeatures = true;

                try
                {
                    bool success = DisablePowerSavingFeatures.TogglePowerSavingFeatures(value);
                    if (success)
                    {
                        OnPropertyChanged(nameof(DisablePowerSavingFeature));
                    }
                    else
                    {
                        OnPropertyChanged(nameof(DisablePowerSavingFeature));
                    }
                }
                finally
                {
                    _isSettingDisablePowerSavingFeatures = false;
                }
            }
        }

        public bool IntelOptimizationsEnabled
        {
            get => IntelCPUOptimization.IsIntelOptimizationEnabled();
            set
            {
                if (_isSettingIntelOptimizations || IntelCPUOptimization.IsIntelOptimizationEnabled() == value)
                    return;

                _isSettingIntelOptimizations = true;

                try
                {
                    bool success = IntelCPUOptimization.ToggleIntelOptimizations(value);
                    if (success)
                    {
                        OnPropertyChanged(nameof(IntelOptimizationsEnabled));
                    }
                    else
                    {
                        OnPropertyChanged(nameof(IntelOptimizationsEnabled));
                    }
                }
                finally
                {
                    _isSettingIntelOptimizations = false;
                }
            }
        }

        public bool AmdOptimizationsEnabled
        {
            get => AmdCpuOptimization.IsAmdOptimizationEnabled();
            set
            {
                if (_isSettingAmdOptimizations || AmdCpuOptimization.IsAmdOptimizationEnabled() == value)
                    return;

                _isSettingAmdOptimizations = true;

                try
                {
                    bool success = AmdCpuOptimization.ToggleAmdOptimizations(value);
                    if (success)
                    {
                        OnPropertyChanged(nameof(AmdOptimizationsEnabled));
                    }
                    else
                    {
                        OnPropertyChanged(nameof(AmdOptimizationsEnabled));
                    }
                }
                finally
                {
                    _isSettingAmdOptimizations = false;
                }
            }
        }

        public void RefreshAllProperties()
        {
            OnPropertyChanged(nameof(RobloxWiFiPriorityBoost));
            OnPropertyChanged(nameof(AllowRobloxFirewall));
            OnPropertyChanged(nameof(GeneralOptimizationsEnabled));
            OnPropertyChanged(nameof(DisablePowerSavingFeature));
            OnPropertyChanged(nameof(IntelOptimizationsEnabled));
            OnPropertyChanged(nameof(AmdOptimizationsEnabled));
        }
    }
}