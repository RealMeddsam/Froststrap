using Bloxstrap.UI.ViewModels.AccountManagers;

namespace Bloxstrap.UI.Elements.AccountManagers.Pages
{
    /// <summary>
    /// Interaction logic for FriendsPage.xaml
    /// </summary>
    public partial class FriendsPage
    {
        public FriendsPage()
        {
            DataContext = new FriendsViewModel();
            InitializeComponent();
        }
    }
}
