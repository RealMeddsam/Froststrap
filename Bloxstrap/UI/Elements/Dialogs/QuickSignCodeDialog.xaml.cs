using System.Windows;
using System.Windows.Threading;

namespace Bloxstrap.UI.Elements.Dialogs
{
    public partial class QuickSignCodeDialog
    {
        public bool SignInSuccessful { get; private set; }
        private DispatcherTimer? _autoCloseTimer;

        public QuickSignCodeDialog()
        {
            InitializeComponent();
            SignInSuccessful = false;

            if (Application.Current?.MainWindow != null)
            {
                Owner = Application.Current.MainWindow;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            CodeBox.Visibility = Visibility.Visible;
            StatusText.Text = "Waiting for Quick Sign-In...\nThe app will close this window when sign-in completes.";
        }

        public void StartNewSignIn(string code)
        {
            SignInSuccessful = false;

            _autoCloseTimer?.Stop();
            _autoCloseTimer = null;

            CodeTextBox.Text = code ?? string.Empty;
            CodeBox.Visibility = Visibility.Visible;
            StatusText.Text = "Waiting for Quick Sign-In...\nCopy the code above and enter it in the Quick Sign-In Page.";

            if (!IsVisible)
            {
                Show();
            }

            Activate();
            Focus();

            InvalidateVisual();
            UpdateLayout();
        }

        public void CompleteSignIn()
        {
            SignInSuccessful = true;
            StatusText.Text = "Login complete! Closing...";

            _autoCloseTimer = new DispatcherTimer();
            _autoCloseTimer.Interval = TimeSpan.FromSeconds(1.5);
            _autoCloseTimer.Tick += (s, e) =>
            {
                _autoCloseTimer?.Stop();
                Close();
            };
            _autoCloseTimer.Start();
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(CodeTextBox.Text);
                StatusText.Text = "Code copied to clipboard!";

                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    StatusText.Text = "Waiting for Quick Sign-In...\nCopy the code above and enter it in the Roblox app.";
                };
                timer.Start();
            }
            catch
            {
                // Ignore clipboard errors
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public void UpdateStatus(string status, string accountName = null!)
        {
            try
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    switch (status)
                    {
                        case "Validated":
                            CompleteSignIn();
                            break;
                        case "Cancelled":
                            StatusText.Text = "Sign-in cancelled.";
                            break;
                        case "TimedOut":
                            StatusText.Text = "Sign-in timed out.";
                            break;
                        case "UserLinked":
                            StatusText.Text = "Device linked - approving sign-in...";
                            break;
                        default:
                            if (!string.IsNullOrEmpty(accountName))
                            {
                                StatusText.Text = $"{status} - {accountName}";
                            }
                            else if (!string.IsNullOrEmpty(status))
                            {
                                StatusText.Text = status;
                            }
                            break;
                    }
                }));
            }
            catch
            {
                // ignore dispatcher errors
            }
        }
    }
}