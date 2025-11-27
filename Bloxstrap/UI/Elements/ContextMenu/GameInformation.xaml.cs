using Bloxstrap.UI.ViewModels.ContextMenu;

namespace Bloxstrap.UI.Elements.ContextMenu
{
    /// <summary>
    /// Interaction logic for GameInformation.xaml
    /// </summary>
    public partial class GameInformation
    {
        public GameInformation(Watcher watcher)
        {
            DataContext = new GameInformationViewModel(watcher);
            InitializeComponent();
        }
    }
}
