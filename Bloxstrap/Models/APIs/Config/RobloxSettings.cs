using System;
using System.Globalization;
using System.Linq;
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

        // Properties for different control types
        public bool IsBoolean => Type == "Boolean";
        public bool IsString => Type == "String";
        public bool IsInt => Type == "Int";
        public bool IsInt64 => Type == "Int64";
        public bool IsFloat => Type == "Float";
        public bool IsToken => Type == "Token";
        public bool IsSecurityCapabilities => Type == "SecurityCapabilities";
        public bool IsVector2 => Type == "Vector2";

        private string[] GetSettingNames()
        {
            return SettingName?
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToArray() ?? Array.Empty<string>();
        }

        public bool BoolValue
        {
            get
            {
                var settingNames = GetSettingNames();
                if (settingNames.Length == 0) return false;

                return settingNames.All(name => RobloxGlobalSettings.GetBool(name));
            }
            set
            {
                var settingNames = GetSettingNames();
                foreach (var name in settingNames)
                {
                    RobloxGlobalSettings.SetBool(name, value);
                }
            }
        }

        public string StringValue
        {
            get
            {
                var settingNames = GetSettingNames();
                if (settingNames.Length == 0) return string.Empty;

                return settingNames
                    .Select(name => RobloxGlobalSettings.GetString(name))
                    .FirstOrDefault(value => !string.IsNullOrEmpty(value)) ?? string.Empty;
            }
            set
            {
                var settingNames = GetSettingNames();
                foreach (var name in settingNames)
                {
                    RobloxGlobalSettings.SetString(name, value);
                }
            }
        }

        public string IntValue
        {
            get
            {
                var settingNames = GetSettingNames();
                if (settingNames.Length == 0) return "0";

                var values = settingNames.Select(name => RobloxGlobalSettings.GetInt(name));
                return values.FirstOrDefault(v => v != 0).ToString();
            }
            set
            {
                if (string.IsNullOrEmpty(value) || !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                    return;

                var settingNames = GetSettingNames();
                foreach (var name in settingNames)
                {
                    RobloxGlobalSettings.SetInt(name, result);
                }
            }
        }

        public string Int64Value
        {
            get
            {
                var settingNames = GetSettingNames();
                if (settingNames.Length == 0) return "0";

                var values = settingNames.Select(name => RobloxGlobalSettings.GetInt64(name));
                return values.FirstOrDefault(v => v != 0).ToString();
            }
            set
            {
                if (string.IsNullOrEmpty(value) || !long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long result))
                    return;

                var settingNames = GetSettingNames();
                foreach (var name in settingNames)
                {
                    RobloxGlobalSettings.SetInt64(name, result);
                }
            }
        }

        public string FloatValue
        {
            get
            {
                var settingNames = GetSettingNames();
                if (settingNames.Length == 0) return "0";

                var values = settingNames.Select(name => RobloxGlobalSettings.GetFloat(name));
                return values.FirstOrDefault(v => v != 0f).ToString(CultureInfo.InvariantCulture);
            }
            set
            {
                if (string.IsNullOrEmpty(value) || !float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                    return;

                var settingNames = GetSettingNames();
                foreach (var name in settingNames)
                {
                    RobloxGlobalSettings.SetFloat(name, result);
                }
            }
        }

        public string TokenValue
        {
            get
            {
                var settingNames = GetSettingNames();
                if (settingNames.Length == 0) return "0";

                var values = settingNames.Select(name => RobloxGlobalSettings.GetToken(name));
                return values.FirstOrDefault(v => v != 0).ToString();
            }
            set
            {
                var settingNames = GetSettingNames();
                if (string.IsNullOrEmpty(value))
                {
                    foreach (var name in settingNames)
                    {
                        RobloxGlobalSettings.SetToken(name, 0);
                    }
                }
                else if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                {
                    foreach (var name in settingNames)
                    {
                        RobloxGlobalSettings.SetToken(name, result);
                    }
                }
            }
        }

        public string SecurityCapabilitiesValue
        {
            get
            {
                var settingNames = GetSettingNames();
                if (settingNames.Length == 0) return "0";
                var values = settingNames.Select(name => RobloxGlobalSettings.GetSecurityCapabilities(name));
                return values.FirstOrDefault(v => v != 0).ToString();
            }
            set
            {
                var settingNames = GetSettingNames();
                if (string.IsNullOrEmpty(value))
                {
                    foreach (var name in settingNames)
                    {
                        RobloxGlobalSettings.SetSecurityCapabilities(name, 0);
                    }
                }
                else if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                {
                    foreach (var name in settingNames)
                    {
                        RobloxGlobalSettings.SetSecurityCapabilities(name, result);
                    }
                }
            }
        }
        
        public string Vector2XValue
        {
            get
            {
                var settingNames = GetSettingNames();
                if (settingNames.Length == 0) return "0";

                var vec = RobloxGlobalSettings.GetVector2(settingNames[0]);
                return vec.X.ToString(CultureInfo.InvariantCulture);
            }
            set
            {
                var settingNames = GetSettingNames();
                if (settingNames.Length == 0) return;

                if (string.IsNullOrEmpty(value) || !float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                    return;

                var current = RobloxGlobalSettings.GetVector2(settingNames[0]);
                RobloxGlobalSettings.SetVector2(settingNames[0], result, current.Y);
            }
        }

        public string Vector2YValue
        {
            get
            {
                var settingNames = GetSettingNames();
                if (settingNames.Length == 0) return "0";

                var vec = RobloxGlobalSettings.GetVector2(settingNames[0]);
                return vec.Y.ToString(CultureInfo.InvariantCulture);
            }
            set
            {
                var settingNames = GetSettingNames();
                if (settingNames.Length == 0) return;

                if (string.IsNullOrEmpty(value) || !float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                    return;

                var current = RobloxGlobalSettings.GetVector2(settingNames[0]);
                RobloxGlobalSettings.SetVector2(settingNames[0], current.X, result);
            }
        }
    }
}