using System.Windows;

namespace Bloxstrap.Models.APIs.Config
{
    public class RobloxSettings
    {
        public string Name { get; set; } = string.Empty;
        public string Header { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = "String";
        public string SettingName { get; set; } = string.Empty;
        public bool IsBoolean => Type == "Boolean";

        public object Value
        {
            get
            {
                var name = SettingName?
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .FirstOrDefault() ?? string.Empty;

                // Vector2 returned as "X;Y"
                if (Type == "Vector2")
                {
                    var vec = RobloxGlobalSettings.GetVector2(name);
                    return $"{vec.X.ToString(CultureInfo.InvariantCulture)};{vec.Y.ToString(CultureInfo.InvariantCulture)}";
                }

                return Type switch
                {
                    "Int" => RobloxGlobalSettings.GetInt(name),
                    "Int64" => RobloxGlobalSettings.GetInt64(name),
                    "Float" => RobloxGlobalSettings.GetFloat(name),
                    "Boolean" => RobloxGlobalSettings.GetBool(name),
                    "String" => RobloxGlobalSettings.GetString(name),
                    "Token" => RobloxGlobalSettings.GetToken(name),
                    _ => RobloxGlobalSettings.GetString(name)
                };
            }
            set
            {
                try
                {
                    var settingNames = SettingName
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim());

                    foreach (var name in settingNames)
                    {
                        var safeValue = value ?? string.Empty;
                        var strValue = safeValue.ToString()?.Trim() ?? string.Empty;

                        if (Type is "Int" or "Int64" or "Float" or "Token" or "Vector2")
                        {
                            if (string.IsNullOrEmpty(strValue) || strValue == "-")
                                return;
                        }

                        var converted = ConvertValue(safeValue, Type);

                        switch (Type)
                        {
                            case "Int":
                                if (int.TryParse(strValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                                    RobloxGlobalSettings.SetInt(name, i);
                                break;

                            case "Int64":
                                if (long.TryParse(strValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
                                    RobloxGlobalSettings.SetInt64(name, l);
                                break;

                            case "Float":
                                if (float.TryParse(strValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
                                    RobloxGlobalSettings.SetFloat(name, f);
                                break;

                            case "Boolean":
                                RobloxGlobalSettings.SetBool(name, Convert.ToBoolean(converted));
                                break;

                            case "String":
                                RobloxGlobalSettings.SetString(name, Convert.ToString(converted)!);
                                break;

                            case "Token":
                                if (int.TryParse(strValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var t))
                                    RobloxGlobalSettings.SetToken(name, t);
                                break;

                            case "c":
                                if (converted is string s && s.Contains(";"))
                                {
                                    var parts = s.Split(';')
                                                 .Select(p => p.Trim())
                                                 .ToArray();

                                    if (parts.Length == 2 &&
                                        float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
                                        float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
                                    {
                                        RobloxGlobalSettings.SetVector2(name, x, y);
                                    }
                                }
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Frontend.ShowMessageBox(
                        $"Failed to set setting {SettingName} of type {Type}\n{ex.Message}",
                        MessageBoxImage.Warning,
                        MessageBoxButton.OK
                    );
                }
            }
        }

        private object ConvertValue(object value, string type)
        {
            if (value is JsonElement element)
            {
                try
                {
                    return type switch
                    {
                        "Int" => element.GetInt32(),
                        "Int64" => element.GetInt64(),
                        "Float" => element.GetSingle(),
                        "Boolean" => element.GetBoolean(),
                        "String" => element.GetString() ?? string.Empty,
                        "Token" => element.GetInt32(),
                        "Vector2" => element.GetString() ?? string.Empty,
                        _ => element.GetString() ?? string.Empty
                    };
                }
                catch
                {
                    return element.ToString();
                }
            }
            return value;
        }
    }
}