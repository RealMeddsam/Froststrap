using Bloxstrap.UI.ViewModels.Settings;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for BloxstrapPage.xaml
    /// </summary>
    public partial class BloxstrapPage
    {
        public BloxstrapPage()
        {
            DataContext = new BloxstrapViewModel();
            InitializeComponent();
        }
    }
}