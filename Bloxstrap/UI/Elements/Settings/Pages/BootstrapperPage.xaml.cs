using Bloxstrap.UI.ViewModels.Settings;
using Bloxstrap.UI.Elements.Dialogs;
using System.Windows;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for BehaviourPage.xaml
    /// </summary>
    public partial class BehaviourPage
    {
        public BehaviourPage()
        {
            DataContext = new BehaviourViewModel();
            InitializeComponent();
            App.FrostRPC?.SetPage("Bootstrapper");
        }

        private void RemoveSelectedProcessExclusion_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is BehaviourViewModel viewModel && viewModel.IsProcessSelected)
            {
                viewModel.RemoveProcessExclusion(viewModel.SelectedProcess);
            }
        }

        private void AddProcessExclusion_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is BehaviourViewModel viewModel)
            {
                string processName = ProcessNameTextBox.Text;
                if (!string.IsNullOrWhiteSpace(processName))
                {
                    viewModel.AddProcessExclusion(processName);
                }
            }
        }

        private void SaveProcessEdit_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is BehaviourViewModel viewModel && viewModel.IsProcessSelected)
            {
                string newName = viewModel.EditProcessName;
                if (!string.IsNullOrWhiteSpace(newName))
                {
                    viewModel.UpdateProcessExclusion(viewModel.SelectedProcess, newName);
                }
            }
        }

        private void OpenMultiblox_Click(object sender, RoutedEventArgs e)
        {
            var window = new MultibloxDialog
            {
                Owner = Window.GetWindow(this)
            };
            window.ShowDialog();
        }
    }
}
