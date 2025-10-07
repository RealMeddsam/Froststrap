using System.Collections.ObjectModel;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class RobloxSettingsViewModel : NotifyPropertyChangedViewModel
    {
        public ObservableCollection<RobloxSettings> NormalRobloxSettings { get; set; } = new();
        public ObservableCollection<RobloxSettings> HiddenRobloxSettings { get; set; } = new();

        public void LoadFromRemote(RemoteDataBase remoteData)
        {
            NormalRobloxSettings.Clear();
            HiddenRobloxSettings.Clear();

            if (remoteData.NormalRobloxSettings != null)
            {
                foreach (var kvp in remoteData.NormalRobloxSettings)
                {
                    var map = kvp.Value;
                    NormalRobloxSettings.Add(new RobloxSettings
                    {
                        Name = kvp.Key,
                        Header = map.Header,
                        Description = map.Description,
                        Type = map.Type,
                        SettingName = map.SettingName
                    });
                }
            }

            if (remoteData.HiddenRobloxSettings != null)
            {
                foreach (var kvp in remoteData.HiddenRobloxSettings)
                {
                    var map = kvp.Value;
                    HiddenRobloxSettings.Add(new RobloxSettings
                    {
                        Name = kvp.Key,
                        Header = map.Header,
                        Description = map.Description,
                        Type = map.Type,
                        SettingName = map.SettingName
                    });
                }
            }
        }
    }
}