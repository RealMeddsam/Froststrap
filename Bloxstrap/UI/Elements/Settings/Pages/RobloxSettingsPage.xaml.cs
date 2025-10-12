using Bloxstrap.UI.Elements.Dialogs;
using Bloxstrap.UI.ViewModels.Settings;
using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    public partial class RobloxSettingsPage : UiPage
    {
        private readonly RobloxSettingsViewModel viewModel;

        public RobloxSettingsPage()
        {
            InitializeComponent();
            viewModel = new RobloxSettingsViewModel();
            DataContext = viewModel;

            this.Loaded += RobloxSettingsPage_Loaded;

            // Load settings from remote data
            App.RemoteData.Subscribe((sender, e) =>
            {
                viewModel.LoadFromRemote(App.RemoteData.Prop);
            });
        }

        private void RobloxSettingsPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            (App.Current as App)?._froststrapRPC?.UpdatePresence("Page: Roblox Settings");
        }

        private void OpenRobloxSettingsDialog_Click(object sender, RoutedEventArgs e)
        {
            (App.Current as App)?._froststrapRPC?.UpdatePresence("Dialog: Roblox Settings");

            var dialog = new RobloxSettingsDialog();
            dialog.Owner = Window.GetWindow(this);

            dialog.ShowDialog();

            (App.Current as App)?._froststrapRPC?.UpdatePresence("Page: Roblox Settings");
        }

        #region Validation Methods

        // Validation for Int32 (allows positive and negative integers)
        private void ValidateInt32(object sender, TextCompositionEventArgs e)
        {
            if (!(sender is System.Windows.Controls.TextBox textBox))
                return;

            string newText = GetNewText(textBox, e.Text);
            e.Handled = !int.TryParse(newText, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
        }

        // Validation for Int64 (allows positive and negative integers)
        private void ValidateInt64(object sender, TextCompositionEventArgs e)
        {
            if (!(sender is System.Windows.Controls.TextBox textBox))
                return;

            string newText = GetNewText(textBox, e.Text);
            e.Handled = !long.TryParse(newText, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
        }

        // Validation for Float (allows decimals and negative numbers)
        private void ValidateFloat(object sender, TextCompositionEventArgs e)
        {
            if (!(sender is System.Windows.Controls.TextBox textBox))
                return;

            string newText = GetNewText(textBox, e.Text);

            // Allow empty string for backspace/delete
            if (string.IsNullOrEmpty(newText))
            {
                e.Handled = false;
                return;
            }

            // Allow single minus sign at start
            if (newText == "-")
            {
                e.Handled = false;
                return;
            }

            // Allow decimal point
            if (newText.EndsWith(".") && newText.Count(c => c == '.') == 1)
            {
                e.Handled = false;
                return;
            }

            e.Handled = !float.TryParse(newText, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
        }

        // Validation for UInt32 (only positive integers)
        private void ValidateUInt32(object sender, TextCompositionEventArgs e)
        {
            if (!(sender is System.Windows.Controls.TextBox textBox))
                return;

            string newText = GetNewText(textBox, e.Text);
            e.Handled = !uint.TryParse(newText, out _);
        }

        // Helper method to get the new text after input
        private string GetNewText(System.Windows.Controls.TextBox textBox, string inputText)
        {
            if (textBox == null)
                return inputText ?? "";

            string currentText = textBox.Text ?? "";
            int selectionStart = textBox.SelectionStart;
            int selectionLength = textBox.SelectionLength;

            // Ensure selection is within bounds
            if (selectionStart < 0) selectionStart = 0;
            if (selectionStart > currentText.Length) selectionStart = currentText.Length;
            if (selectionLength < 0) selectionLength = 0;
            if (selectionStart + selectionLength > currentText.Length)
                selectionLength = currentText.Length - selectionStart;

            // Remove selected text and insert new text
            string newText = currentText.Remove(selectionStart, selectionLength).Insert(selectionStart, inputText ?? "");
            return newText;
        }

        #endregion
    }
}
