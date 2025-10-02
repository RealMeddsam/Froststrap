using System.Xml.Linq;
using System.Windows;

namespace Bloxstrap
{
    public static class FpsUnlocker
    {
        private static readonly string SettingsPath = Path.Combine(Paths.RobloxGlobal, "GlobalBasicSettings_13.xml");

        public static int GetFpsCap()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return -1;
                var doc = XDocument.Load(SettingsPath);
                var fpsElement = FindFPSElement(doc);
                if (fpsElement != null && int.TryParse(fpsElement.Value, out int fps))
                    return fps;
            }
            catch { }
            return -1;
        }

        public static void SetFpsCap(int fpsCap)
        {
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    var newDoc = new XDocument(
                        new XElement("robloxSettings",
                            new XElement("int", new XAttribute("name", "FramerateCap"), fpsCap.ToString())
                        )
                    );
                    Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                    newDoc.Save(SettingsPath);
                    return;
                }

                var doc = XDocument.Load(SettingsPath);
                var fpsElement = FindFPSElement(doc);

                if (fpsElement != null)
                    fpsElement.Value = fpsCap.ToString();
                else
                    doc.Root?.Add(new XElement("int", new XAttribute("name", "FramerateCap"), fpsCap.ToString()));

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