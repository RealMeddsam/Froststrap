﻿using Bloxstrap.RobloxInterfaces;
using Bloxstrap.UI.Elements.Bootstrapper.Base;
using Bloxstrap.UI.ViewModels.Bootstrapper;
using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Shell;
using System.Windows.Threading;

namespace Bloxstrap.UI.Elements.Bootstrapper
{
    /// <summary>
    /// Interaction logic for FluentDialog.xaml
    /// </summary>
    public partial class FluentDialog : IBootstrapperDialog
    {
        private readonly FluentDialogViewModel _viewModel;

        public Bloxstrap.Bootstrapper? Bootstrapper { get; set; }

        private bool _isClosing;
        public string VersionText { get; init; } = "None";
        public string ChannelText { get; init; } = "production";

        #region UI Elements
        public string Message
        {
            get => _viewModel.Message;
            set
            {
                _viewModel.Message = value;
                _viewModel.OnPropertyChanged(nameof(_viewModel.Message));
            }
        }

        public ProgressBarStyle ProgressStyle
        {
            get => _viewModel.ProgressIndeterminate ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;
            set
            {
                _viewModel.ProgressIndeterminate = (value == ProgressBarStyle.Marquee);
                _viewModel.OnPropertyChanged(nameof(_viewModel.ProgressIndeterminate));
            }
        }

        public int ProgressMaximum
        {
            get => _viewModel.ProgressMaximum;
            set
            {
                _viewModel.ProgressMaximum = value;
                _viewModel.OnPropertyChanged(nameof(_viewModel.ProgressMaximum));
            }
        }

        public int ProgressValue
        {
            get => _viewModel.ProgressValue;
            set
            {
                _viewModel.ProgressValue = value;
                _viewModel.OnPropertyChanged(nameof(_viewModel.ProgressValue));
            }
        }

        public TaskbarItemProgressState TaskbarProgressState
        {
            get => _viewModel.TaskbarProgressState;
            set
            {
                _viewModel.TaskbarProgressState = value;
                _viewModel.OnPropertyChanged(nameof(_viewModel.TaskbarProgressState));
            }
        }

        public double TaskbarProgressValue
        {
            get => _viewModel.TaskbarProgressValue;
            set
            {
                _viewModel.TaskbarProgressValue = value;
                _viewModel.OnPropertyChanged(nameof(_viewModel.TaskbarProgressValue));
            }
        }

        public bool CancelEnabled
        {
            get => _viewModel.CancelEnabled;
            set
            {
                _viewModel.CancelEnabled = value;

                _viewModel.OnPropertyChanged(nameof(_viewModel.CancelButtonVisibility));
                _viewModel.OnPropertyChanged(nameof(_viewModel.CancelEnabled));
            }
        }
        #endregion

        public FluentDialog(bool aero)
        {
            InitializeComponent();

            string version = Utilities.GetRobloxVersionStr(Bootstrapper?.IsStudioLaunch ?? false);
            string channel = Deployment.Channel;
            _viewModel = new FluentDialogViewModel(this, aero, version, channel);
            DataContext = _viewModel;
            Title = App.Settings.Prop.BootstrapperTitle;
            Icon = App.Settings.Prop.BootstrapperIcon.GetIcon().GetImageSource();

            // setting this to true for mica results in the window being undraggable
            if (aero)
                AllowsTransparency = true;

            VersionText = $"{Strings.Common_Version}: {version}";
            ChannelText = $"{Strings.Common_Channel}: {channel}";
        }

        private void UiWindow_Closing(object sender, CancelEventArgs e)
        {
            if (!_isClosing)
                Bootstrapper?.Cancel();
        }

        #region IBootstrapperDialog Methods
        public void ShowBootstrapper() => this.ShowDialog();

        public void CloseBootstrapper()
        {
            _isClosing = true;
            Dispatcher.BeginInvoke(this.Close);
        }

        public void ShowSuccess(string message, Action? callback) => BaseFunctions.ShowSuccess(message, callback);
        #endregion
    }
}