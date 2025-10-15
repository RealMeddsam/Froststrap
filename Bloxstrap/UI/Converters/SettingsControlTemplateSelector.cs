using System.Windows;
using System.Windows.Controls;
using Bloxstrap.Models.APIs.Config;

namespace Bloxstrap.UI.Converters
{
    public class SettingsControlTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is SettingsControl control && container is FrameworkElement frameworkElement)
            {
                string resourceName = control.Type switch
                {
                    ControlType.TextBox => "TextBoxTemplate",
                    ControlType.Slider => "SliderTemplate",
                    ControlType.ToggleSwitch => "ToggleSwitchTemplate",
                    ControlType.ComboBox => "ComboBoxTemplate",
                    ControlType.Vector2 => "Vector2Template",
                    _ => "TextBoxTemplate"
                };

                if (frameworkElement.TryFindResource(resourceName) is DataTemplate template)
                {
                    return template;
                }
            }

            return base.SelectTemplate(item, container);
        }
    }
}