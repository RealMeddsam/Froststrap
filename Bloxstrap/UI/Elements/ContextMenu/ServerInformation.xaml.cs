using Bloxstrap.UI.ViewModels.ContextMenu;

namespace Bloxstrap.UI.Elements.ContextMenu
{
    /// <summary>
    /// Interaction logic for ServerInformation.xaml
    /// </summary>
    public partial class ServerInformation
    {
        public ServerInformation(Watcher watcher)
        {
            DataContext = new ServerInformationViewModel(watcher);
            InitializeComponent();
        }
    }
}
