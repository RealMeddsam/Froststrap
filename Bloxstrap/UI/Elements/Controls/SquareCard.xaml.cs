using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Bloxstrap.UI.Elements.Controls
{
    [ContentProperty(nameof(InnerContent))]
    public partial class SquareCard : UserControl, INotifyPropertyChanged
    {
        public enum CategoryType
        {
            Performance,
            Privacy,
            Cpu,
            Gpu,
            Network,
            Warning
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(SquareCard));

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(SquareCard));

        public static readonly DependencyProperty InnerContentProperty =
            DependencyProperty.Register(nameof(InnerContent), typeof(object), typeof(SquareCard));

        public static readonly DependencyProperty CategoriesProperty =
            DependencyProperty.Register(nameof(Categories), typeof(string), typeof(SquareCard),
                new PropertyMetadata("", OnCategoriesChanged));

        public static readonly DependencyProperty WarningToolTipProperty =
            DependencyProperty.Register(nameof(WarningToolTip), typeof(string), typeof(SquareCard),
                new PropertyMetadata("", OnCategoriesChanged));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
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

        public string Categories
        {
            get => (string)GetValue(CategoriesProperty);
            set => SetValue(CategoriesProperty, value);
        }

        public string WarningToolTip
        {
            get => (string)GetValue(WarningToolTipProperty);
            set => SetValue(WarningToolTipProperty, value);
        }

        public ObservableCollection<CategoryIconInfo> CategoryIcons
        {
            get
            {
                var icons = new ObservableCollection<CategoryIconInfo>();

                if (!string.IsNullOrEmpty(Categories))
                {
                    var categoryStrings = Categories.Split(',')
                        .Select(c => c.Trim())
                        .Where(c => !string.IsNullOrEmpty(c));

                    foreach (var categoryString in categoryStrings)
                    {
                        if (Enum.TryParse<CategoryType>(categoryString, true, out var category))
                        {
                            icons.Add(new CategoryIconInfo
                            {
                                Category = category,
                                Icon = GetCategoryIcon(category),
                                BorderColor = GetCategoryBorderColor(category),
                                ToolTip = GetCategoryToolTip(category)
                            });
                        }
                    }
                }

                return icons;
            }
        }

        private static void OnCategoriesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (SquareCard)d;
            control.OnPropertyChanged(nameof(CategoryIcons));
        }

        private BitmapImage GetCategoryIcon(CategoryType category)
        {
            string iconName = category switch
            {
                CategoryType.Performance => "Performance",
                CategoryType.Privacy => "Privacy",
                CategoryType.Cpu => "CPU",
                CategoryType.Gpu => "GPU",
                CategoryType.Network => "Network",
                CategoryType.Warning => "Warning",
                _ => "Performance"
            };

            return new BitmapImage(new Uri($"/Resources/PCTweaks/{iconName}.png", UriKind.Relative));
        }

        private SolidColorBrush GetCategoryBorderColor(CategoryType category)
        {
            return category switch
            {
                CategoryType.Performance => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ffef24")),
                CategoryType.Privacy => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f9de70")),
                CategoryType.Cpu => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#b772fb")),
                CategoryType.Gpu => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#abfb72")),
                CategoryType.Network => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#70d3f9")),
                CategoryType.Warning => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f97070")),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#000000"))
            };
        }

        private string GetCategoryToolTip(CategoryType category)
        {
            return category switch
            {
                CategoryType.Warning => WarningToolTip,
                CategoryType.Performance => "Performance Optimization",
                CategoryType.Privacy => "Privacy & Security",
                CategoryType.Cpu => "CPU Optimization",
                CategoryType.Gpu => "GPU Optimization",
                CategoryType.Network => "Network Optimization",
                _ => "Unknown Category"
            };
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

    public class CategoryIconInfo
    {
        public SquareCard.CategoryType Category { get; set; }
        public BitmapImage Icon { get; set; } = null!;
        public SolidColorBrush BorderColor { get; set; } = null!;
        public string ToolTip { get; set; } = string.Empty;
    }
}