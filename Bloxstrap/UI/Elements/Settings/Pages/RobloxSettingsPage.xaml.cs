using Bloxstrap.UI.Elements.Dialogs;
using Bloxstrap.UI.ViewModels.Settings;
using System.Windows;
using Wpf.Ui.Controls;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    public partial class RobloxSettingsPage : UiPage
    {
        private readonly RobloxSettingsViewModel viewModel;

        public RobloxSettingsPage()
        {
            InitializeComponent();
            viewModel = new RobloxSettingsViewModel();
            DataContext = viewModel;

            this.Loaded += RobloxSettingsPage_Loaded;

            // Load settings from remote data
            App.RemoteData.Subscribe((sender, e) =>
            {
                viewModel.LoadFromRemote(App.RemoteData.Prop);
            });
        }

        private void RobloxSettingsPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            (App.Current as App)?._froststrapRPC?.UpdatePresence("Page: Roblox Settings");
        }

        private void OpenRobloxSettingsDialog_Click(object sender, RoutedEventArgs e)
        {
            (App.Current as App)?._froststrapRPC?.UpdatePresence("Dialog: Roblox Settings");

            var dialog = new RobloxSettingsDialog();
            dialog.Owner = Window.GetWindow(this);

            dialog.ShowDialog();

            (App.Current as App)?._froststrapRPC?.UpdatePresence("Page: Roblox Settings");
        }
    }
}
