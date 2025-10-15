using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Bloxstrap.Models.APIs.Config;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class RobloxSettingsViewModel : INotifyPropertyChanged
    {
        private readonly RemoteDataManager _remoteDataManager;

        public ObservableCollection<SettingsSection> Sections { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public RobloxSettingsViewModel(RemoteDataManager remoteDataManager)
        {
            _remoteDataManager = remoteDataManager;
            _remoteDataManager.Subscribe(OnRemoteDataLoaded);
        }

        private void OnRemoteDataLoaded(object? sender, EventArgs e)
        {
            LoadFromRemoteConfig();
            LoadCurrentValuesFromGBS();
        }

        public void LoadFromRemoteConfig()
        {
            Sections.Clear();
            var pageConfig = _remoteDataManager.Prop.SettingsPage;

            foreach (var sectionConfig in pageConfig.Sections)
            {
                Sections.Add(sectionConfig);
            }

            SubscribeToControlChanges();
            OnPropertyChanged(nameof(Sections));
        }

        private void SubscribeToControlChanges()
        {
            foreach (var section in Sections)
            {
                foreach (var control in section.Controls)
                {
                    control.PropertyChanged += OnControlPropertyChanged;
                }
            }
        }

        private void OnControlPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is SettingsControl control && control.GBSConfig != null)
            {
                IEnumerable<string> paths = control.GBSConfig.XmlPaths.Count > 0
                    ? control.GBSConfig.XmlPaths
                    : new[] { control.GBSConfig.XmlPath };

                foreach (var path in paths)
                {
                    App.GlobalSettings.SetValue(path, control.GBSConfig.DataType, control.Value);
                }
            }
        }

        private void LoadCurrentValuesFromGBS()
        {
            App.GlobalSettings.Load();

            foreach (var section in Sections)
            {
                foreach (var control in section.Controls)
                {
                    if (control.GBSConfig != null)
                    {
                        IEnumerable<string> paths = control.GBSConfig.XmlPaths.Count > 0
                            ? control.GBSConfig.XmlPaths
                            : new[] { control.GBSConfig.XmlPath };

                        if (control.Type == ControlType.ToggleSwitch && paths.Count() > 1)
                        {
                            // Logical AND of all values
                            bool allTrue = paths.All(p =>
                                App.GlobalSettings.GetValue(p, "bool")?.ToLower() == "true"
                            );

                            control.Value = allTrue.ToString().ToLower();
                        }
                        else if (!string.IsNullOrEmpty(control.GBSConfig.XmlPath))
                        {
                            var currentValue = App.GlobalSettings.GetValue(control.GBSConfig.XmlPath, control.GBSConfig.DataType);
                            control.Value = !string.IsNullOrEmpty(currentValue) ? currentValue : GetDefaultValueForControl(control);
                        }
                    }
                }
            }
        }

        private string GetDefaultValueForControl(SettingsControl control)
        {
            return control.Type switch
            {
                ControlType.Slider => control.MinValue.ToString(),
                ControlType.ToggleSwitch => "false",
                ControlType.ComboBox when control.Options.Count > 0 => control.Options[0].Value,
                ControlType.Vector2 => "0,0",
                _ => ""
            };
        }

        public bool ReadOnly
        {
            get => App.GlobalSettings.GetReadOnly();
            set => App.GlobalSettings.SetReadOnly(value);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}