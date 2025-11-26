using System;
using Bloxstrap.UI.ViewModels.Dialogs;

namespace Bloxstrap.UI.Elements.Dialogs
{
    public partial class MultibloxWindow
    {
        private readonly MultibloxViewModel _viewModel = new();

        public MultibloxWindow()
        {
            InitializeComponent();
            DataContext = _viewModel;
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel.Dispose();
            App.Settings.Save();
            base.OnClosed(e);
        }
    }
}
