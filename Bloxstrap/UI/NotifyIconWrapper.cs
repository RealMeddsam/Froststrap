using Bloxstrap.Integrations;
using Bloxstrap.UI.Elements.About;
using Bloxstrap.UI.Elements.ContextMenu;
using System.Windows;
using Wpf.Ui.Controls;

namespace Bloxstrap.UI
{
    public class NotifyIconWrapper : IDisposable
    {
        // lol who needs properly structured mvvm and xaml when you have the absolute catastrophe that this is

        private bool _disposing = false;

        private readonly System.Windows.Forms.NotifyIcon _notifyIcon;
        
        private readonly MenuContainer _menuContainer;
        
        private readonly Watcher _watcher;

        private ActivityWatcher? _activityWatcher => _watcher.ActivityWatcher;

        EventHandler? _alertClickHandler;

        public NotifyIconWrapper(Watcher watcher)
        {
            App.Logger.WriteLine("NotifyIconWrapper::NotifyIconWrapper", "Initializing notification area icon");

            _watcher = watcher;

            _notifyIcon = new(new System.ComponentModel.Container())
            {
                Icon = Properties.Resources.IconBloxstrap,
                Text = "Froststrap",
                Visible = true
            };

            _notifyIcon.MouseClick += MouseClickEventHandler;

            _notifyIcon.MouseDoubleClick += (s, e) =>
            {
                if (e.Button != System.Windows.Forms.MouseButtons.Left)
                    return;

                switch (App.Settings.Prop.DoubleClickAction)
                {
                    case TrayDoubleClickAction.None:
                        Frontend.ShowMessageBox(
                             "You dont have the double click action set to anything",
                            MessageBoxImage.Information
                        );
                        break;

                    case TrayDoubleClickAction.DebugMenu:
                        var debugMenu = new DebugMenu
                        {
                            Topmost = true
                        };
                        debugMenu.Show();
                        debugMenu.Activate();
                        break;

                    case TrayDoubleClickAction.GameHistory:
                        if (!App.Settings.Prop.ShowGameHistoryMenu)
                        {
                            Frontend.ShowMessageBox(
                                "Enable 'Game History' in settings to use this feature.",
                                MessageBoxImage.Information
                            );
                            return;
                        }

                        _menuContainer!.Dispatcher.Invoke(() =>
                        {
                            _menuContainer.GameHistoryMenuItem.RaiseEvent(
                                new RoutedEventArgs(MenuItem.ClickEvent));

                            var win = Application.Current.Windows
                                .OfType<ServerHistory>()
                                .FirstOrDefault();
                            if (win != null)
                            {
                                win.Topmost = true;
                                win.Activate();
                            }
                        });
                        break;

                    case TrayDoubleClickAction.ServerInfo:
                        if (!App.Settings.Prop.ShowServerDetails)
                        {
                            Frontend.ShowMessageBox(
                                "Enable 'Query Server Location' in settings to use this feature.",
                                MessageBoxImage.Information
                            );
                            return;
                        }

                        if (_activityWatcher is not null && _activityWatcher.InGame)
                        {
                            _menuContainer!.ShowServerInformationWindow();

                            var win = Application.Current.Windows
                                .OfType<ServerInformation>()
                                .FirstOrDefault();
                            if (win != null)
                            {
                                win.Topmost = true;
                                win.Activate();
                            }
                        }
                        else
                        {
                            Frontend.ShowMessageBox(
                                "Join a game first to view server information.",
                                MessageBoxImage.Information
                            );
                        }
                        break;

                    case TrayDoubleClickAction.LogsMenu:
                        if (App.FastFlags.GetPreset("Players.LogLevel") != "trace")
                        {
                            Frontend.ShowMessageBox(
                                "Enable 'Logs Menu' to use the logs menu.",
                                MessageBoxImage.Information
                            );
                            return;
                        }

                        if (_activityWatcher is not null && _activityWatcher.InGame)
                        {
                            _menuContainer!.Dispatcher.Invoke(() =>
                            {
                                _menuContainer.LogsMenuItem.RaiseEvent(
                                    new RoutedEventArgs(MenuItem.ClickEvent));

                                var win = Application.Current.Windows
                                    .OfType<Logs>()
                                    .FirstOrDefault();
                                if (win != null)
                                {
                                    win.Topmost = true;
                                    win.Activate();
                                }
                            });
                        }
                        else
                        {
                            Frontend.ShowMessageBox(
                                "Join a game first to view logs.",
                                MessageBoxImage.Information
                            );
                        }
                        break;
                }
            };

            if (_activityWatcher is not null && App.Settings.Prop.ShowServerDetails)
                _activityWatcher.OnGameJoin += OnGameJoin;

            _menuContainer = new(_watcher);
            _menuContainer.Show();
        }

        #region Context menu
        public void MouseClickEventHandler(object? sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button != System.Windows.Forms.MouseButtons.Right)
                return;

            _menuContainer.Activate();
            _menuContainer.ContextMenu.IsOpen = true;
        }
        #endregion

        #region Activity handlers
        public async void OnGameJoin(object? sender, EventArgs e)
        {
            if (_activityWatcher is null)
                return;
            
            string? serverLocation = await _activityWatcher.Data.QueryServerLocation();

            if (string.IsNullOrEmpty(serverLocation))
                return;

            string title = _activityWatcher.Data.ServerType switch
            {
                ServerType.Public => Strings.ContextMenu_ServerInformation_Notification_Title_Public,
                ServerType.Private => Strings.ContextMenu_ServerInformation_Notification_Title_Private,
                ServerType.Reserved => Strings.ContextMenu_ServerInformation_Notification_Title_Reserved,
                _ => ""
            };

            ShowAlert(
                title,
                String.Format(Strings.ContextMenu_ServerInformation_Notification_Text, serverLocation),
                10,
                (_, _) => _menuContainer.ShowServerInformationWindow()
            );
        }
        #endregion

        // we may need to create our own handler for this, because this sorta sucks
        public void ShowAlert(string caption, string message, int duration, EventHandler? clickHandler)
        {
            string id = Guid.NewGuid().ToString()[..8];

            string LOG_IDENT = $"NotifyIconWrapper::ShowAlert.{id}";

            App.Logger.WriteLine(LOG_IDENT, $"Showing alert for {duration} seconds (clickHandler={clickHandler is not null})");
            App.Logger.WriteLine(LOG_IDENT, $"{caption}: {message.Replace("\n", "\\n")}");

            _notifyIcon.BalloonTipTitle = caption;
            _notifyIcon.BalloonTipText = message;

            if (_alertClickHandler is not null)
            {
                App.Logger.WriteLine(LOG_IDENT, "Previous alert still present, erasing click handler");
                _notifyIcon.BalloonTipClicked -= _alertClickHandler;
            }

            _alertClickHandler = clickHandler;
            _notifyIcon.BalloonTipClicked += clickHandler;

            _notifyIcon.ShowBalloonTip(duration);

            Task.Run(async () =>
            {
                await Task.Delay(duration * 1000);
             
                _notifyIcon.BalloonTipClicked -= clickHandler;

                App.Logger.WriteLine(LOG_IDENT, "Duration over, erasing current click handler");

                if (_alertClickHandler == clickHandler)
                    _alertClickHandler = null;
                else
                    App.Logger.WriteLine(LOG_IDENT, "Click handler has been overridden by another alert");
            });
        }

        public void Dispose()
        {
            if (_disposing)
                return;

            _disposing = true;

            App.Logger.WriteLine("NotifyIconWrapper::Dispose", "Disposing NotifyIcon");

            _menuContainer.Dispatcher.Invoke(_menuContainer.Close);
            _notifyIcon.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
