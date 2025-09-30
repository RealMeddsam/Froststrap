using Bloxstrap.UI.ViewModels.Settings;
using Wpf.Ui.Controls;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    public partial class FastFlagsPage : UiPage
    {
        public FastFlagsPage()
        {
            InitializeComponent();
            DataContext = new FastFlagsViewModel();
            (App.Current as App)?._froststrapRPC?.UpdatePresence("Page: FastFlags Settings");
        }
    }
}