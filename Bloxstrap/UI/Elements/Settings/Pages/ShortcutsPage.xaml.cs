using Bloxstrap.UI.ViewModels.Settings;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for ShortcutsPage.xaml
    /// </summary>
    public partial class ShortcutsPage
    {
        public ShortcutsPage()
        {
            DataContext = new ShortcutsViewModel();
            InitializeComponent();
            App.FrostRPC?.SetPage("Shortcut");
        }
    }
}
