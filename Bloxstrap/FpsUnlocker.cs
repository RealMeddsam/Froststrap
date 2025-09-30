using System.Xml.Linq;
using System.Windows;

namespace Bloxstrap
{
    public static class FpsUnlocker
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Roblox",
            "GlobalBasicSettings_13.xml"
        );

        public static bool IsUncapped()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return false;
                var doc = XDocument.Load(SettingsPath);
                var fpsElement = FindFPSElement(doc);
                return fpsElement != null && int.TryParse(fpsElement.Value, out int fps) && fps > 240;
            }
            catch { return false; }
        }

        public static void SetUncapped(bool uncap)
        {
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    var newDoc = new XDocument(
                        new XElement("robloxSettings",
                            new XElement("int", new XAttribute("name", "FramerateCap"), uncap ? "9999" : "-1")
                        )
                    );
                    Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                    newDoc.Save(SettingsPath);
                    return;
                }

                var doc = XDocument.Load(SettingsPath);
                var fpsElement = FindFPSElement(doc);

                if (fpsElement != null)
                    fpsElement.Value = uncap ? "9999" : "-1";
                else
                    doc.Root?.Add(new XElement("int", new XAttribute("name", "FramerateCap"), uncap ? "9999" : "-1"));

                doc.Save(SettingsPath);
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox($"Failed to set FPS cap:\n{ex.Message}", MessageBoxImage.Error, MessageBoxButton.OK);
            }
        }

        private static XElement? FindFPSElement(XDocument doc)
        {
            foreach (var intElement in doc.Descendants("int"))
            {
                var nameAttr = intElement.Attribute("name");
                if (nameAttr != null && nameAttr.Value == "FramerateCap")
                    return intElement;
            }
            return null;
        }
    }
}