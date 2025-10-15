using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Bloxstrap.UI.Elements.Controls
{
    [ContentProperty(nameof(InnerContent))]
    public partial class SquareCard : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(nameof(Header), typeof(string), typeof(SquareCard));

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(SquareCard));

        public static readonly DependencyProperty InnerContentProperty =
            DependencyProperty.Register(nameof(InnerContent), typeof(object), typeof(SquareCard));

        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public object InnerContent
        {
            get => GetValue(InnerContentProperty);
            set => SetValue(InnerContentProperty, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public SquareCard()
        {
            InitializeComponent();
        }
    }
}