using Bloxstrap.UI.ViewModels.Settings;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for AccountManagerPage.xaml
    /// </summary>
    public partial class AccountManagerPage : UiPage
    {
        private AccountManagerViewModel? _viewModel;

        public AccountManagerPage()
        {
            _viewModel = new AccountManagerViewModel();
            DataContext = _viewModel;

            InitializeComponent();

            App.FrostRPC?.SetPage("Account Manager");
        }

        private void MoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel?.ShowChangeAccountDialogCommand is not null && _viewModel.ShowChangeAccountDialogCommand.CanExecute(null))
            {
                _viewModel.ShowChangeAccountDialogCommand.Execute(null);
                return;
            }

            if (_viewModel != null)
                _viewModel.IsChangeAccountDialogOpen = true;
        }

        private void AccountsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel?.SelectAccountCommand is not null && _viewModel.SelectAccountCommand.CanExecute(null))
            {
                _viewModel.SelectAccountCommand.Execute(null);
            }
        }

        private void AddMethodsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox lb && lb.SelectedItem != null)
            {
                if (_viewModel != null)
                {
                    _viewModel.SelectedAddMethod = lb.SelectedItem.ToString() ?? _viewModel.SelectedAddMethod;
                }

                try
                {
                    if (AddMethodToggle != null)
                        AddMethodToggle.IsChecked = false;
                }
                catch
                {
                    // ignore failures
                }
            }
        }

        private void AddNewAccount_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not AccountManagerViewModel vm)
                return;

            try
            {
                if (vm.AddAccountCommand.CanExecute(null))
                    vm.AddAccountCommand.Execute(null);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("AccountManagerPage::AddNewAccount_Click", ex);
            }
        }

        private void AccountsListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Check if the click is on the delete button - if so, don't start drag
            if (e.OriginalSource is FrameworkElement source)
            {
                var current = source;
                while (current != null)
                {
                    if (current is Wpf.Ui.Controls.Button button && button.Command == _viewModel?.DeleteAccountCommand)
                    {
                        return;
                    }
                    current = VisualTreeHelper.GetParent(current) as FrameworkElement;
                }
            }

            var listBox = sender as ListBox;
            if (listBox == null) return;

            var item = ItemsControl.ContainerFromElement(listBox, e.OriginalSource as DependencyObject) as ListBoxItem;
            if (item != null)
            {
                var account = item.Content as Account;
                if (account != null && _viewModel?.StartDragCommand?.CanExecute(account) == true)
                {
                    _viewModel.StartDragCommand.Execute(account);
                }
            }
        }

        private void AccountsListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _viewModel?.DraggedAccount != null)
            {
                DragDrop.DoDragDrop(AccountsListBox, _viewModel.DraggedAccount, DragDropEffects.Move);
                if (_viewModel?.EndDragCommand?.CanExecute(null) == true)
                {
                    _viewModel.EndDragCommand.Execute(null);
                }
            }
        }

        private void AccountsListBox_Drop(object sender, DragEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox == null) return;

            var targetItem = ItemsControl.ContainerFromElement(listBox, e.OriginalSource as DependencyObject) as ListBoxItem;
            if (targetItem != null)
            {
                var targetAccount = targetItem.Content as Account;
                if (targetAccount != null && _viewModel?.DropOnAccountCommand?.CanExecute(targetAccount) == true)
                {
                    _viewModel.DropOnAccountCommand.Execute(targetAccount);
                }
            }
        }

        private void AccountsListBox_DragOver(object sender, DragEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox == null) return;

            foreach (var item in listBox.Items)
            {
                var container = listBox.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
                if (container != null)
                {
                    container.BorderBrush = Brushes.Transparent;
                }
            }

            var targetItem = ItemsControl.ContainerFromElement(listBox, e.OriginalSource as DependencyObject) as ListBoxItem;
            if (targetItem != null && _viewModel?.DraggedAccount != null)
            {
                var targetAccount = targetItem.Content as Account;
                if (targetAccount != null && targetAccount.Id != _viewModel.DraggedAccount.Id)
                {
                    targetItem.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D4"));
                    targetItem.BorderThickness = new Thickness(2);
                }
            }

            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private void SearchComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && !string.IsNullOrEmpty(_viewModel.PlaceId))
            {
                SearchComboBox.Text = _viewModel.PlaceId;
            }
        }

        private void SearchComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_viewModel?.PersistPlaceIdCommand?.CanExecute(null) == true)
            {
                _viewModel.PersistPlaceIdCommand.Execute(null);
            }
        }

        private void ServerIdTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_viewModel?.PersistServerIdCommand?.CanExecute(null) == true)
            {
                _viewModel.PersistServerIdCommand.Execute(null);
            }
        }

        private void DeleteButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            var button = sender as Wpf.Ui.Controls.Button;
            if (button?.Command != null && button.Command.CanExecute(button.CommandParameter))
            {
                button.Command.Execute(button.CommandParameter);
            }
        }

        private void HorizontalScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer && !e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = UIElement.MouseWheelEvent,
                    Source = sender
                };
                var parent = VisualTreeHelper.GetParent(scrollViewer) as UIElement;
                parent?.RaiseEvent(eventArg);
            }
        }
    }
}