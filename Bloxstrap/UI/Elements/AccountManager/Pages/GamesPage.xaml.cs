using Bloxstrap.UI.ViewModels.AccountManagers;

namespace Bloxstrap.UI.Elements.AccountManagers.Pages
{
    /// <summary>
    /// Interaction logic for GamesPage.xaml
    /// </summary>
    public partial class GamesPage
    {
        private GamesViewModel? _viewModel;

        public GamesPage()
        {
            _viewModel = new GamesViewModel();
            DataContext = _viewModel;
            InitializeComponent();
        }
    }
}