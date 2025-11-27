using Bloxstrap.UI.ViewModels.AccountManagers;
using Bloxstrap.UI.ViewModels.Settings;
using System.Windows;

namespace Bloxstrap.UI.Elements.AccountManagers.Pages
{
    public partial class AccountsPage
    {
        private AccountsViewModel? _viewModel;

        public AccountsPage()
        {
            DataContext = new AccountsViewModel();
            InitializeComponent();
            _viewModel = DataContext as AccountsViewModel;
        }

        private void AccountManagerPage_Loaded(object? sender, RoutedEventArgs e)
        {
            if (DataContext is AccountManagerViewModel vm)
                vm.OpenAccountManagerWindowIfEnabled();
        }
    }
}