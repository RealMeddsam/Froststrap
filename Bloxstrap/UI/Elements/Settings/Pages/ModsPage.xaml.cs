using Bloxstrap.Integrations;
using Bloxstrap.RobloxInterfaces;
using Bloxstrap.UI.ViewModels.Settings;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for ModsPage.xaml
    /// </summary>
    public partial class ModsPage
    {
        private string? CustomLogoPath = null;

        private ModsViewModel ViewModel;

        public ModsPage()
        {
            InitializeComponent();
            ViewModel = new ModsViewModel();
            DataContext = ViewModel;
            (App.Current as App)?._froststrapRPC?.UpdatePresence("Page: Mods");

            IncludeModificationsCheckBox.IsChecked = true;

            ViewModel.GradientStops.Add(new GradientStopViewModel { Offset = 0, ColorHex = "#60B4DC" });
        }

        private async void ModGenerator_Click(object sender, RoutedEventArgs e)
        {
            GenerateModButton.IsEnabled = false;
            AddStopButton.IsEnabled = false;

            DownloadStatusText.Text = "Starting mod generation...";

            try
            {
                var (luaPackagesDir, extraTexturesDir, contentTexturesDir, versionHash, version) =
                    await Deployment.DownloadForModGenerator();

                DownloadStatusText.Text = "Download complete!\nCleaning up unnecessary files...";

                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                Dictionary<string, string[]> mappings;
                using (var stream = assembly.GetManifestResourceStream("Bloxstrap.Resources.mappings.json"))
                using (var reader = new StreamReader(stream!))
                {
                    string json = await reader.ReadToEndAsync();
                    mappings = JsonSerializer.Deserialize<Dictionary<string, string[]>>(json)!;
                }

                string froststrapTemp = Path.Combine(Path.GetTempPath(), "Froststrap");
                var preservePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Path.Combine(froststrapTemp, @"ExtraContent\LuaPackages\Packages\_Index\FoundationImages\FoundationImages\SpriteSheets")
        };

                foreach (var entry in mappings.Values)
                {
                    string fullPath = Path.Combine(froststrapTemp, Path.Combine(entry));
                    preservePaths.Add(fullPath);
                }

                string foundationImagesDir = Path.Combine(froststrapTemp, @"ExtraContent\LuaPackages\Packages\_Index\FoundationImages\FoundationImages");
                string? getImageSetDataPath = Directory.EnumerateFiles(foundationImagesDir, "GetImageSetData.lua", SearchOption.AllDirectories).FirstOrDefault();

                if (getImageSetDataPath != null)
                {
                    string renamedPath = Path.Combine(Path.GetDirectoryName(getImageSetDataPath)!, "GetImageSetData.lua");
                    if (!File.Exists(renamedPath))
                    {
                        File.Move(getImageSetDataPath, renamedPath);
                    }
                    preservePaths.Add(renamedPath);
                }

                void DeleteExcept(string dir)
                {
                    foreach (var file in Directory.GetFiles(dir))
                    {
                        if (!preservePaths.Contains(file))
                        {
                            File.Delete(file);
                        }
                    }

                    foreach (var subDir in Directory.GetDirectories(dir))
                    {
                        if (!preservePaths.Contains(subDir))
                        {
                            DeleteExcept(subDir);
                            if (Directory.Exists(subDir) && !Directory.EnumerateFileSystemEntries(subDir).Any())
                            {
                                Directory.Delete(subDir);
                            }
                        }
                    }
                }

                DeleteExcept(luaPackagesDir);
                DeleteExcept(extraTexturesDir);
                DeleteExcept(contentTexturesDir);

                Color? solidColor = null;
                List<ModGenerator.GradientStop>? gradient = null;

                if (ViewModel.GradientStops.Count == 1)
                {
                    solidColor = ViewModel.GradientStops[0].Color;
                }
                else
                {
                    gradient = ViewModel.GradientStops.Select(s => new ModGenerator.GradientStop(s.Offset, s.Color)).ToList();
                }

                DownloadStatusText.Text = "Recoloring images...";
                ModGenerator.RecolorAllPngs(froststrapTemp, solidColor, gradient, getImageSetDataPath ?? string.Empty, CustomLogoPath);

                string infoPath = Path.Combine(froststrapTemp, "info.json");
                var infoData = new
                {
                    FroststrapVersion = App.Version,
                    CreatedUsing = "Froststrap",
                    RobloxVersion = version,
                    RobloxVersionHash = versionHash
                };

                string infoJson = JsonSerializer.Serialize(infoData, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(infoPath, infoJson);

                App.Logger.WriteLine("UI::ModGenerator", $"info.json created at {infoPath}");

                if (IncludeModificationsCheckBox.IsChecked == true)
                {
                    if (!Directory.Exists(Paths.Modifications))
                        Directory.CreateDirectory(Paths.Modifications);

                    foreach (var dir in new[] { froststrapTemp })
                    {
                        foreach (var file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                        {
                            string relativePath = Path.GetRelativePath(dir, file);
                            string destPath = Path.Combine(Paths.Modifications, relativePath);
                            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                            File.Copy(file, destPath, overwrite: true);
                        }
                    }

                    DownloadStatusText.Text = $"Mod files copied to {Paths.Modifications}";
                }
                else
                {
                    var saveDialog = new SaveFileDialog
                    {
                        FileName = "FroststrapMod.zip",
                        Filter = "ZIP Archives (*.zip)|*.zip",
                        Title = "FroststrapMod"
                    };

                    if (saveDialog.ShowDialog() == true)
                    {
                        ModGenerator.ZipResult(froststrapTemp, saveDialog.FileName);
                        DownloadStatusText.Text = $"Mod generated successfully! Saved to: {saveDialog.FileName}";
                    }
                    else
                    {
                        DownloadStatusText.Text = "Save cancelled by user.";
                    }
                }
            }
            catch (Exception ex)
            {
                DownloadStatusText.Text = $"Error: {ex.Message}";
                App.Logger.WriteException("UI::ModGenerator", ex);
            }
            finally
            {
                GenerateModButton.IsEnabled = true;
                AddStopButton.IsEnabled = true;
            }
        }

        private void OnSelectCustomLogo_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
                Title = "Select Custom Roblox Logo"
            };

            if (dlg.ShowDialog() == true)
            {
                CustomLogoPath = dlg.FileName;
                SelectedLogoText.Text = $"Selected: {Path.GetFileName(dlg.FileName)}";
            }
        }

        private void OnClearCustomLogo_Click(object sender, RoutedEventArgs e)
        {
            CustomLogoPath = null;
            SelectedLogoText.Text = "No custom logo selected";
        }


        #region Gradient Color Stuff
        public class GradientStopViewModel : INotifyPropertyChanged
        {
            private float offset;
            private string colorHex = "#60B4DC";

            public float Offset
            {
                get => offset;
                set { offset = value; OnPropertyChanged(); }
            }

            public string ColorHex
            {
                get => colorHex;
                set { colorHex = value; OnPropertyChanged(); }
            }

            public Color Color
            {
                get
                {
                    try { return ColorTranslator.FromHtml(colorHex); }
                    catch { return Color.White; }
                }
                set { ColorHex = $"#{value.R:X2}{value.G:X2}{value.B:X2}"; }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void OnAddGradientStop_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.GradientStops.Add(new GradientStopViewModel { Offset = 1f, ColorHex = "#60B4DC" });
        }

        private void OnRemoveGradientStop_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is GradientStopViewModel stop)
            {
                ViewModel.GradientStops.Remove(stop);
            }
        }

        private void OnMoveUpGradientStop_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is GradientStopViewModel stop)
            {
                int idx = ViewModel.GradientStops.IndexOf(stop);
                if (idx > 0)
                    ViewModel.GradientStops.Move(idx, idx - 1);
            }
        }

        private void OnMoveDownGradientStop_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is GradientStopViewModel stop)
            {
                int idx = ViewModel.GradientStops.IndexOf(stop);
                if (idx < ViewModel.GradientStops.Count - 1)
                    ViewModel.GradientStops.Move(idx, idx + 1);
            }
        }

        private void OnGradientOffsetChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Two-way binding handles this automatically
        }

        private void OnGradientColorHexChanged(object sender, TextChangedEventArgs e)
        {
            // Two-way binding handles this automatically
        }

        private void OnChangeGradientColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is GradientStopViewModel stop)
            {
                var dlg = new System.Windows.Forms.ColorDialog
                {
                    AllowFullOpen = true,
                    FullOpen = true,
                    Color = ColorTranslator.FromHtml(stop.ColorHex)
                };

                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    stop.ColorHex = $"#{dlg.Color.R:X2}{dlg.Color.G:X2}{dlg.Color.B:X2}";
                }
            }
        }
        #endregion
    }
}