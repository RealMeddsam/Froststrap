using System.Collections.ObjectModel;
using System.Windows;
using System.ComponentModel;
using System.Windows.Data;

namespace Bloxstrap.UI.Elements.Dialogs
{
    /// <summary>
    /// Interaction logic for FlagProfilesDialog.xaml
    /// </summary>
    public partial class RobloxSettingsDialog
    {
        public ObservableCollection<RobloxSettingEntry> Settings { get; } = new();

        public ICollectionView FilteredSettings { get; }

        public RobloxSettingsDialog()
        {
            InitializeComponent();
            DataContext = this;

            foreach (var (type, name, value) in RobloxGlobalSettings.GetAllSettings())
                Settings.Add(new RobloxSettingEntry { Type = type, Name = name, Value = value });

            FilteredSettings = CollectionViewSource.GetDefaultView(Settings);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var entry in Settings)
            {
                switch (entry.Type)
                {
                    case "Vector2":
                        var parts = entry.Value.Split(';');
                        float x = 0, y = 0;

                        if (parts.Length == 2)
                        {
                            float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x);
                            float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y);
                        }

                        RobloxGlobalSettings.SetVector2(entry.Name, x, y);

                        entry.Value = $"{x.ToString(CultureInfo.InvariantCulture)};{y.ToString(CultureInfo.InvariantCulture)}";
                        break;

                    default:
                        RobloxGlobalSettings.SetValue(entry.Name, entry.Type, entry.Value);
                        break;
                }
            }
        }

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox tb)
            {
                string filter = tb.Text.ToLowerInvariant();

                FilteredSettings.Filter = obj =>
                {
                    if (obj is RobloxSettingEntry entry)
                    {
                        return entry.Name.ToLowerInvariant().Contains(filter) ||
                               entry.Type.ToLowerInvariant().Contains(filter);
                    }
                    return false;
                };
            }
        }
    }
}