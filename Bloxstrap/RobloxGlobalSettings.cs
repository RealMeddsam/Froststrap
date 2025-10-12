using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Windows;

namespace Bloxstrap
{
    public static class RobloxGlobalSettings
    {
        private static readonly string SettingsPath = Path.Combine(Paths.Roblox, "GlobalBasicSettings_13.xml");

        public static string? GetValue(string name, string type) => GetRaw(name, type);
        public static void SetValue(string name, string type, string value) => SetRaw(name, type, value);

        private static XDocument LoadDocument()
        {
            if (!File.Exists(SettingsPath))
            {
                var doc = new XDocument(new XElement("robloxSettings"));
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                doc.Save(SettingsPath);
                return doc;
            }
            return XDocument.Load(SettingsPath);
        }

        private static XElement? FindElement(XDocument doc, string name, string type)
        {
            foreach (var el in doc.Descendants(type))
            {
                var nameAttr = el.Attribute("name");
                if (nameAttr != null && nameAttr.Value == name)
                    return el;
            }
            return null;
        }

        private static string? GetRaw(string name, string type)
        {
            try
            {
                var doc = LoadDocument();
                var element = FindElement(doc, name, type);
                return element?.Value?.Trim();
            }
            catch { return null; }
        }

        private static void SetRaw(string name, string type, string value)
        {
            try
            {
                var doc = LoadDocument();
                var element = FindElement(doc, name, type);

                if (element != null)
                    element.Value = value;
                else
                    doc.Root?.Add(new XElement(type, new XAttribute("name", name), value));

                doc.Save(SettingsPath);
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox($"Failed to set Roblox setting:\n{ex.Message}", MessageBoxImage.Error, MessageBoxButton.OK);
            }
        }

        // --- Supported Types ---
        public static int GetInt(string name, int fallback = 0) =>
            int.TryParse(GetRaw(name, "int"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : fallback;
        public static void SetInt(string name, int value) =>
            SetRaw(name, "int", value.ToString(CultureInfo.InvariantCulture));

        public static long GetInt64(string name, long fallback = 0) =>
            long.TryParse(GetRaw(name, "int64"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : fallback;
        public static void SetInt64(string name, long value) =>
            SetRaw(name, "int64", value.ToString(CultureInfo.InvariantCulture));

        public static float GetFloat(string name, float fallback = 0f) =>
            float.TryParse(GetRaw(name, "float"), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var fResult) ? fResult : fallback;
        public static void SetFloat(string name, float value) =>
            SetRaw(name, "float", value.ToString(CultureInfo.InvariantCulture));

        public static bool GetBool(string name, bool fallback = false) =>
            bool.TryParse(GetRaw(name, "bool"), out var bResult) ? bResult : fallback;
        public static void SetBool(string name, bool value) =>
            SetRaw(name, "bool", value.ToString().ToLowerInvariant());

        public static string GetString(string name, string fallback = "") =>
            GetRaw(name, "string") ?? fallback;
        public static void SetString(string name, string value) =>
            SetRaw(name, "string", value);

        public static string GetBinaryString(string name, string fallback = "") =>
            GetRaw(name, "BinaryString") ?? fallback;
        public static void SetBinaryString(string name, string value) =>
            SetRaw(name, "BinaryString", value);

        public static int GetToken(string name, int fallback = 0) =>
            int.TryParse(GetRaw(name, "token"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var tResult) ? tResult : fallback;
        public static void SetToken(string name, int value) =>
            SetRaw(name, "token", value.ToString(CultureInfo.InvariantCulture));

        public static int GetSecurityCapabilities(string name, int fallback = 0) =>
            int.TryParse(GetRaw(name, "SecurityCapabilities"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var sResult) ? sResult : fallback;
        public static void SetSecurityCapabilities(string name, int value) =>
            SetRaw(name, "SecurityCapabilities", value.ToString(CultureInfo.InvariantCulture));

        // --- Vector2 ---
        public static (float X, float Y) GetVector2(string name, float fallbackX = 0f, float fallbackY = 0f)
        {
            try
            {
                var doc = LoadDocument();
                var el = FindElement(doc, name, "Vector2");
                if (el != null)
                {
                    var xStr = el.Element("X")?.Value;
                    var yStr = el.Element("Y")?.Value;

                    float.TryParse(xStr, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var x);
                    float.TryParse(yStr, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var y);

                    return (x, y);
                }
            }
            catch { }
            return (fallbackX, fallbackY);
        }

        public static void SetVector2(string name, float x, float y)
        {
            try
            {
                var doc = LoadDocument();
                var el = FindElement(doc, name, "Vector2");
                if (el != null)
                {
                    el.SetElementValue("X", x.ToString(CultureInfo.InvariantCulture));
                    el.SetElementValue("Y", y.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    var vector2 = new XElement("Vector2", new XAttribute("name", name),
                        new XElement("X", x.ToString(CultureInfo.InvariantCulture)),
                        new XElement("Y", y.ToString(CultureInfo.InvariantCulture))
                    );
                    doc.Root?.Add(vector2);
                }
                doc.Save(SettingsPath);
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox($"Failed to set Roblox Vector2:\n{ex.Message}", MessageBoxImage.Error, MessageBoxButton.OK);
            }
        }

        // --- GetAllSettings ---
        public static IEnumerable<(string Type, string Name, string Value)> GetAllSettings()
        {
            var doc = LoadDocument();

            // Return all elements with a 'name' attribute, including Vector2
            foreach (var el in doc.Descendants().Where(e => e.Attribute("name") != null))
            {
                var name = el.Attribute("name")!.Value;
                var type = el.Name.LocalName;
                string value;

                if (type == "Vector2")
                {
                    var xStr = el.Element("X")?.Value ?? "0";
                    var yStr = el.Element("Y")?.Value ?? "0";

                    if (!float.TryParse(xStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var x))
                        x = 0;
                    if (!float.TryParse(yStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
                        y = 0;

                    value = $"{x.ToString(CultureInfo.InvariantCulture)};{y.ToString(CultureInfo.InvariantCulture)}";
                }
                else
                {
                    value = el.Value?.Trim() ?? "";
                }

                yield return (type, name, value);
            }
        }
    }
}