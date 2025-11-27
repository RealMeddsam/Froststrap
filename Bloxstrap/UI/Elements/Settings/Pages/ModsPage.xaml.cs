using Bloxstrap.Integrations;
using Bloxstrap.RobloxInterfaces;
using Bloxstrap.UI.ViewModels.Settings;
using Microsoft.Win32;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Compression;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Drawing.Drawing2D;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for ModsPage.xaml
    /// </summary>
    public partial class ModsPage
    {
        private ModsViewModel ViewModel;
        private Color _solidColor = Color.White;

        public ModsPage()
        {
            InitializeComponent();
            ViewModel = new ModsViewModel();
            DataContext = ViewModel;
            App.FrostRPC?.SetPage("Mods");

            InitializePreview();
            IncludeModificationsCheckBox.IsChecked = true;

            SolidColorTextBox.Text = "#FFFFFF";
        }


        // Preserve the font files in mappings.json so they dont get deleting after generating the mod, its in resources
        private async void ModGenerator_Click(object sender, RoutedEventArgs e)
        {
            const string LOG_IDENT = "UI::ModGenerator";
            var overallSw = Stopwatch.StartNew();

            GenerateModButton.IsEnabled = false;

            DownloadStatusText.Text = "Starting mod generation...";
            App.Logger?.WriteLine(LOG_IDENT, "Mod generation started.");

            try
            {
                var (luaPackagesZip, extraTexturesZip, contentTexturesZip, versionHash, version) =
                    await Deployment.DownloadForModGenerator();

                App.Logger?.WriteLine(LOG_IDENT, $"DownloadForModGenerator returned. Version: {version} ({versionHash})");

                string froststrapTemp = Path.Combine(Path.GetTempPath(), "Froststrap");

                string luaPackagesDir = Path.Combine(froststrapTemp, "ExtraContent", "LuaPackages");
                string extraTexturesDir = Path.Combine(froststrapTemp, "ExtraContent", "textures");
                string contentTexturesDir = Path.Combine(froststrapTemp, "content", "textures");

                void SafeExtract(string zipPath, string targetDir)
                {
                    if (Directory.Exists(targetDir))
                    {
                        try { Directory.Delete(targetDir, true); }
                        catch (Exception ex)
                        {
                            App.Logger?.WriteException(LOG_IDENT, ex);
                            throw;
                        }
                    }

                    Directory.CreateDirectory(targetDir);

                    using (var archive = ZipFile.OpenRead(zipPath))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            if (string.IsNullOrEmpty(entry.FullName) || entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\"))
                                continue;

                            string destinationPath = Path.GetFullPath(Path.Combine(targetDir, entry.FullName));

                            if (!destinationPath.StartsWith(Path.GetFullPath(targetDir), StringComparison.OrdinalIgnoreCase))
                                throw new IOException($"Entry {entry.FullName} is trying to extract outside of {targetDir}");

                            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                            entry.ExtractToFile(destinationPath, overwrite: true);
                        }
                    }
                }

                DownloadStatusText.Text = "Extracting ZIPs...";
                App.Logger?.WriteLine(LOG_IDENT, "Extracting downloaded ZIPs...");

                SafeExtract(luaPackagesZip, luaPackagesDir);
                SafeExtract(extraTexturesZip, extraTexturesDir);
                SafeExtract(contentTexturesZip, contentTexturesDir);

                App.Logger?.WriteLine(LOG_IDENT, "Extraction complete.");

                var assembly = Assembly.GetExecutingAssembly();
                Dictionary<string, string[]> mappings;
                using (var stream = assembly.GetManifestResourceStream("Bloxstrap.Resources.mappings.json"))
                using (var reader = new StreamReader(stream!))
                {
                    string json = await reader.ReadToEndAsync();
                    mappings = JsonSerializer.Deserialize<Dictionary<string, string[]>>(json)!;
                }
                App.Logger?.WriteLine(LOG_IDENT, $"Loaded mappings.json with {mappings.Count} top-level entries.");

                DownloadStatusText.Text = "Recoloring images...";

                bool colorCursors = CursorsCheckBox?.IsChecked == true;
                bool colorShiftlock = ShiftlockCheckBox?.IsChecked == true;
                bool colorEmoteWheel = EmoteWheelCheckBox?.IsChecked == true;
                bool colorVoiceChat = VoiceChatCheckBox?.IsChecked == true;

                App.Logger?.WriteLine(LOG_IDENT, $"Using solid color for recolor: {_solidColor}");

                App.Logger?.WriteLine(LOG_IDENT, "Starting RecolorAllPngs...");
                ModGenerator.RecolorAllPngs(froststrapTemp, _solidColor, colorCursors, colorShiftlock, colorEmoteWheel, colorVoiceChat);
                App.Logger?.WriteLine(LOG_IDENT, "RecolorAllPngs finished.");

                DownloadStatusText.Text = "Cleaning up unnecessary files...";
                var preservePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var entry in mappings.Values)
                {
                    string fullPath = Path.Combine(froststrapTemp, Path.Combine(entry));
                    preservePaths.Add(fullPath);
                }

                if (colorCursors)
                {
                    preservePaths.Add(Path.Combine(froststrapTemp, @"content\textures\Cursors\KeyboardMouse\IBeamCursor.png"));
                    preservePaths.Add(Path.Combine(froststrapTemp, @"content\textures\Cursors\KeyboardMouse\ArrowCursor.png"));
                    preservePaths.Add(Path.Combine(froststrapTemp, @"content\textures\Cursors\KeyboardMouse\ArrowFarCursor.png"));
                }

                if (colorShiftlock)
                {
                    preservePaths.Add(Path.Combine(froststrapTemp, @"content\textures\MouseLockedCursor.png"));
                }

                if (colorEmoteWheel)
                {
                    string emotesDir = Path.Combine(froststrapTemp, @"content\textures\ui\Emotes\Large");
                    preservePaths.Add(Path.Combine(emotesDir, "SelectedGradient.png"));
                    preservePaths.Add(Path.Combine(emotesDir, "SelectedGradient@2x.png"));
                    preservePaths.Add(Path.Combine(emotesDir, "SelectedGradient@3x.png"));
                    preservePaths.Add(Path.Combine(emotesDir, "SelectedLine.png"));
                    preservePaths.Add(Path.Combine(emotesDir, "SelectedLine@2x.png"));
                    preservePaths.Add(Path.Combine(emotesDir, "SelectedLine@3x.png"));
                }

                if (colorVoiceChat)
                {
                    var voiceChatMappings = new Dictionary<string, (string BaseDir, string[] Files)>
                    {
                        ["VoiceChat"] = (
                            @"content\textures\ui\VoiceChat",
                            new[]
                            {"Blank.png","Blank@2x.png","Blank@3x.png","Error.png","Error@2x.png","Error@3x.png","Muted.png","Muted@2x.png","Muted@3x.png","Unmuted0.png","Unmuted0@2x.png","Unmuted0@3x.png","Unmuted20.png","Unmuted20@2x.png","Unmuted20@3x.png","Unmuted40.png","Unmuted40@2x.png","Unmuted40@3x.png","Unmuted60.png","Unmuted60@2x.png","Unmuted60@3x.png","Unmuted80.png","Unmuted80@2x.png","Unmuted80@3x.png","Unmuted100.png","Unmuted100@2x.png","Unmuted100@3x.png"}
                        ),
                        ["SpeakerNew"] = (
                            @"content\textures\ui\VoiceChat\SpeakerNew",
                            new[]
                            {"Unmuted60@3x.png","Unmuted80.png","Unmuted80@2x.png","Unmuted80@3x.png","Unmuted100.png","Unmuted100@2x.png","Unmuted100@3x.png","Error.png","Error@2x.png","Error@3x.png","Muted.png","Muted@2x.png","Muted@3x.png","Unmuted0.png","Unmuted0@2x.png","Unmuted0@3x.png","Unmuted20.png","Unmuted20@2x.png","Unmuted20@3x.png","Unmuted40.png","Unmuted40@2x.png","Unmuted40@3x.png","Unmuted60.png","Unmuted60@2x.png"}
                        ),
                        ["SpeakerLight"] = (
                            @"content\textures\ui\VoiceChat\SpeakerLight",
                            new[]
                            {"Muted@2x.png","Muted@3x.png","Unmuted0.png","Unmuted0@2x.png","Unmuted0@3x.png","Unmuted20.png","Unmuted20@2x.png","Unmuted20@3x.png","Unmuted40.png","Unmuted40@2x.png","Unmuted40@3x.png","Unmuted60.png","Unmuted60@2x.png","Unmuted60@3x.png","Unmuted80.png","Unmuted80@2x.png","Unmuted80@3x.png","Unmuted100.png","Unmuted100@2x.png","Unmuted100@3x.png","Error.png","Error@2x.png","Error@3x.png","Muted.png"}
                        ),
                        ["SpeakerDark"] = (
                            @"content\textures\ui\VoiceChat\SpeakerDark",
                            new[]
                            {"Unmuted40.png","Unmuted40@2x.png","Unmuted40@3x.png","Unmuted60.png","Unmuted60@2x.png","Unmuted60@3x.png","Unmuted80.png","Unmuted80@2x.png","Unmuted80@3x.png","Unmuted100.png","Unmuted100@2x.png","Unmuted100@3x.png","Error.png","Error@2x.png","Error@3x.png","Muted.png","Muted@2x.png","Muted@3x.png","Unmuted0.png","Unmuted0@2x.png","Unmuted0@3x.png","Unmuted20.png","Unmuted20@2x.png","Unmuted20@3x.png"}
                        ),
                        ["RedSpeakerLight"] = (
                            @"content\textures\ui\VoiceChat\RedSpeakerLight",
                            new[]
                            {"Unmuted20.png","Unmuted20@2x.png","Unmuted20@3x.png","Unmuted40.png","Unmuted40@2x.png","Unmuted40@3x.png","Unmuted60.png","Unmuted60@2x.png","Unmuted60@3x.png","Unmuted80.png","Unmuted80@2x.png","Unmuted80@3x.png","Unmuted100.png","Unmuted100@2x.png","Unmuted100@3x.png","Unmuted0.png","Unmuted0@2x.png","Unmuted0@3x.png"}
                        ),
                        ["RedSpeakerDark"] = (
                            @"content\textures\ui\VoiceChat\RedSpeakerDark",
                            new[]
                            {"Unmuted20.png","Unmuted20@2x.png","Unmuted20@3x.png","Unmuted40.png","Unmuted40@2x.png","Unmuted40@3x.png","Unmuted60.png","Unmuted60@2x.png","Unmuted60@3x.png","Unmuted80.png","Unmuted80@2x.png","Unmuted80@3x.png","Unmuted100.png","Unmuted100@2x.png","Unmuted100@3x.png","Unmuted0.png","Unmuted0@2x.png","Unmuted0@3x.png"}
                        ),
                        ["New"] = (
                            @"content\textures\ui\VoiceChat\New",
                            new[]
                            {"Error.png","Error@2x.png","Error@3x.png", "Unmuted0.png","Unmuted0@2x.png","Unmuted0@3x.png","Unmuted20.png","Unmuted20@2x.png","Unmuted20@3x.png","Unmuted40.png","Unmuted40@2x.png","Unmuted40@3x.png","Unmuted60.png","Unmuted60@2x.png","Unmuted60@3x.png","Unmuted80.png","Unmuted80@2x.png","Unmuted80@3x.png","Unmuted100.png","Unmuted100@2x.png","Unmuted100@3x.png","Blank.png","Blank@2x.png","Blank@3x.png"}
                        ),
                        ["MicLight"] = (
                            @"content\textures\ui\VoiceChat\MicLight",
                            new[]
                            {"Error.png","Error@2x.png","Error@3x.png","Muted.png","Muted@2x.png","Muted@3x.png","Unmuted0.png","Unmuted0@2x.png","Unmuted0@3x.png","Unmuted20.png","Unmuted20@2x.png","Unmuted20@3x.png","Unmuted40.png","Unmuted40@2x.png","Unmuted40@3x.png","Unmuted60.png","Unmuted60@2x.png","Unmuted60@3x.png","Unmuted80.png","Unmuted80@2x.png","Unmuted80@3x.png","Unmuted100.png","Unmuted100@2x.png","Unmuted100@3x.png"}
                        ),
                        ["MicDark"] = (
                            @"content\textures\ui\VoiceChat\MicDark",
                            new[]
                            {"Muted.png","Muted@2x.png","Muted@3x.png","Unmuted0.png","Unmuted0@2x.png","Unmuted0@3x.png","Unmuted20.png","Unmuted20@2x.png","Unmuted20@3x.png","Unmuted40.png","Unmuted40@2x.png","Unmuted40@3x.png","Unmuted60.png","Unmuted60@2x.png","Unmuted60@3x.png","Unmuted80.png","Unmuted80@2x.png","Unmuted80@3x.png","Unmuted100.png","Unmuted100@2x.png","Unmuted100@3x.png","Error.png","Error@2x.png","Error@3x.png"}
                        )
                    };

                    foreach (var mapping in voiceChatMappings.Values)
                    {
                        string baseDir = Path.Combine(froststrapTemp, mapping.BaseDir);
                        foreach (var file in mapping.Files)
                        {
                            preservePaths.Add(Path.Combine(baseDir, file));
                        }
                    }
                }

                void DeleteExcept(string dir)
                {
                    foreach (var file in Directory.GetFiles(dir))
                    {
                        if (!preservePaths.Contains(file))
                        {
                            try { File.Delete(file); } catch { /* ignore */ }
                        }
                    }

                    foreach (var subDir in Directory.GetDirectories(dir))
                    {
                        if (!preservePaths.Contains(subDir))
                        {
                            try
                            {
                                DeleteExcept(subDir);
                                if (Directory.Exists(subDir) && !Directory.EnumerateFileSystemEntries(subDir).Any())
                                    Directory.Delete(subDir);
                            }
                            catch { /* ignore */ }
                        }
                    }
                }

                if (Directory.Exists(luaPackagesDir)) DeleteExcept(luaPackagesDir);
                if (Directory.Exists(extraTexturesDir)) DeleteExcept(extraTexturesDir);
                if (Directory.Exists(contentTexturesDir)) DeleteExcept(contentTexturesDir);

                string infoPath = Path.Combine(froststrapTemp, "info.json");
                var infoData = new
                {
                    FroststrapVersion = App.Version,
                    CreatedUsing = "Froststrap",
                    RobloxVersion = version,
                    RobloxVersionHash = versionHash,
                    OptionsUsed = new
                    {
                        ColorCursors = colorCursors,
                        ColorShiftlock = colorShiftlock,
                        ColorVoicechat = colorVoiceChat,
                        ColorEmoteWheel = colorEmoteWheel,
                    },
                    ColorsUsed = new
                    {
                        SolidColor = $"#{_solidColor.R:X2}{_solidColor.G:X2}{_solidColor.B:X2}"
                    }
                };

                string infoJson = JsonSerializer.Serialize(infoData, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(infoPath, infoJson);

                if (IncludeModificationsCheckBox.IsChecked == true)
                {
                    if (!Directory.Exists(Paths.Modifications))
                        Directory.CreateDirectory(Paths.Modifications);

                    int copiedFiles = 0;
                    foreach (var file in Directory.GetFiles(froststrapTemp, "*", SearchOption.AllDirectories))
                    {
                        if (Path.GetExtension(file).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                            continue;

                        string relativePath = Path.GetRelativePath(froststrapTemp, file);
                        string destPath = Path.Combine(Paths.Modifications, relativePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                        File.Copy(file, destPath, overwrite: true);
                        copiedFiles++;
                    }

                    DownloadStatusText.Text = $"Mod files copied to {Paths.Modifications}";
                    App.Logger?.WriteLine(LOG_IDENT, $"Copied {copiedFiles} files to {Paths.Modifications}");
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
                        App.Logger?.WriteLine(LOG_IDENT, $"Mod zip created at {saveDialog.FileName}");
                    }
                    else
                    {
                        DownloadStatusText.Text = "Save cancelled by user.";
                        App.Logger?.WriteLine(LOG_IDENT, "User cancelled save dialog.");
                    }
                }

                overallSw.Stop();
                App.Logger?.WriteLine(LOG_IDENT, $"Mod generation completed successfully in {overallSw.ElapsedMilliseconds} ms.");
            }
            catch (Exception ex)
            {
                DownloadStatusText.Text = $"Error: {ex.Message}";
                App.Logger?.WriteException(LOG_IDENT, ex);
            }
            finally
            {
                GenerateModButton.IsEnabled = true;
            }
        }

        private void OnSolidColorChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                string colorHex = SolidColorTextBox.Text.Trim();
                if (!colorHex.StartsWith("#"))
                    colorHex = "#" + colorHex;

                _solidColor = ColorTranslator.FromHtml(colorHex);
                _ = UpdatePreviewAsync();
            }
            catch
            {
                // Invalid color format, ignore
            }
        }

        private void OnChangeSolidColor_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.ColorDialog
            {
                AllowFullOpen = true,
                FullOpen = true,
                Color = _solidColor
            };

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _solidColor = dlg.Color;
                SolidColorTextBox.Text = $"#{_solidColor.R:X2}{_solidColor.G:X2}{_solidColor.B:X2}";
                _ = UpdatePreviewAsync();
            }
        }

        private Bitmap? _sheetOriginalBitmap = null;
        private CancellationTokenSource? _previewCts;
        private readonly List<SpriteDef> _previewSprites = ParseHardcodedSpriteList();

        private void InitializePreview()
        {
            LoadEmbeddedPreviewSheet();
            PopulateSpriteSelector();
            _ = UpdatePreviewAsync();
        }

        private void LoadEmbeddedPreviewSheet()
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                string? resName = asm.GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith(".png", StringComparison.OrdinalIgnoreCase) && n.Contains("Bloxstrap.Resources"));
                if (resName == null)
                    resName = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
                if (resName == null) return;
                using var rs = asm.GetManifestResourceStream(resName);
                if (rs == null) return;
                using var src = new Bitmap(rs);
                _sheetOriginalBitmap?.Dispose();
                _sheetOriginalBitmap = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(_sheetOriginalBitmap)) g.DrawImage(src, 0, 0, src.Width, src.Height);
            }
            catch (Exception ex)
            {
                App.Logger?.WriteException("ModsPage::LoadEmbeddedPreviewSheet", ex);
            }
        }

        public record SpriteDef(string Name, int X, int Y, int W, int H);

        private static List<SpriteDef> ParseHardcodedSpriteList()
        {
            string[] lines = new[]
            {
                "like_off 0x0 72x72","rocket_off 74x0 72x72","heart_on 148x0 72x72","trophy_off 222x0 72x72","heart_off 296x0 72x72","report_off 370x0 72x72",
                "dislike_off 0x74 72x72","music 74x74 72x72","player_count 148x74 72x72","selfview_on 222x74 72x72","notification_off 296x74 72x72","send 370x74 72x72",
                "like_on 0x148 72x72","robux 74x148 72x72","backpack_on 148x148 72x72","report_on 222x148 72x72","search 296x148 72x72","notification_on 370x148 72x72",
                "dislike_on 0x222 72x72","chat_on 74x222 72x72","backpack_off 148x222 72x72","fingerprint 222x222 72x72","roblox 296x222 72x72","roblox_studio 370x222 72x72",
                "notepad 0x296 72x72","chat_off 74x296 72x72","close 148x296 72x72","add 222x296 72x72","sync 296x296 72x72","pin 370x296 72x72",
                "picture 0x370 72x72","enlarge 74x370 72x72","headset_locked 148x370 72x72","friends_off 222x370 72x72","friends_on 296x370 72x72","person_camera 370x370 72x72"
            };

            var list = new List<SpriteDef>();
            foreach (var l in lines)
            {
                var parts = l.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3) continue;
                string name = parts[0];
                var coords = parts[1].Split('x');
                int x = int.Parse(coords[0]);
                int y = int.Parse(coords[1]);
                var size = parts[2].Split('x');
                int w = int.Parse(size[0]);
                int h = int.Parse(size[1]);
                list.Add(new SpriteDef(name, x, y, w, h));
            }
            return list;
        }

        private void PopulateSpriteSelector()
        {
            SpriteSelector.ItemsSource = _previewSprites.Select(s => s.Name).ToList();
            if (SpriteSelector.Items.Count > 0) SpriteSelector.SelectedIndex = 0;
        }

        private async Task UpdatePreviewAsync()
        {
            _previewCts?.Cancel();
            _previewCts = new CancellationTokenSource();
            var ct = _previewCts.Token;

            try
            {
                if (_sheetOriginalBitmap == null) return;
                byte[] sheetBytes;
                using (var ms = new MemoryStream())
                {
                    _sheetOriginalBitmap.Save(ms, ImageFormat.Png);
                    sheetBytes = ms.ToArray();
                }

                var bmp = await Task.Run(() =>
                {
                    if (ct.IsCancellationRequested) return (Bitmap?)null;

                    try
                    {
                        using var ms2 = new MemoryStream(sheetBytes);
                        using var sheetCopy = new Bitmap(ms2);
                        var result = RenderPreviewSheet(sheetCopy, _solidColor);
                        return result;
                    }
                    catch (Exception ex)
                    {
                        App.Logger?.WriteException("ModsPage::UpdatePreviewAsync(Task)", ex);
                        return (Bitmap?)null;
                    }
                }, ct).ConfigureAwait(false);

                if (bmp == null) return;

                await Dispatcher.InvokeAsync(() =>
                {
                    SheetPreview.Source = BitmapToImageSource(bmp);
                    UpdateSpritePreviewFromBitmap(bmp);
                });

                bmp.Dispose();
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation
            }
            catch (Exception ex)
            {
                App.Logger?.WriteException("ModsPage::UpdatePreviewAsync", ex);
            }
        }

        private Bitmap RenderPreviewSheet(Bitmap sheetBmp, Color solidColor)
        {
            if (sheetBmp == null) throw new InvalidOperationException("sheetBmp is null.");

            var output = new Bitmap(sheetBmp.Width, sheetBmp.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(output))
            {
                g.CompositingMode = CompositingMode.SourceOver;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.DrawImage(sheetBmp, 0, 0);
            }

            try
            {
                foreach (var def in _previewSprites)
                {
                    if (def.W <= 0 || def.H <= 0) continue;
                    var rect = new Rectangle(def.X, def.Y, def.W, def.H);

                    using (var cropped = sheetBmp.Clone(rect, PixelFormat.Format32bppArgb))
                    using (var recolored = ApplyMaskPreview(cropped, solidColor))
                    using (var g = Graphics.FromImage(output))
                    {
                        g.CompositingMode = CompositingMode.SourceOver;
                        g.DrawImage(recolored, rect);
                    }
                }
            }
            finally
            {
                // Clean up if needed
            }

            return output;
        }

        private Bitmap ApplyMaskPreview(Bitmap original, Color solidColor)
        {
            if (original.Width == 0 || original.Height == 0)
                return new Bitmap(original);

            var recolored = new Bitmap(original.Width, original.Height, PixelFormat.Format32bppArgb);
            var rect = new Rectangle(0, 0, original.Width, original.Height);
            BitmapData srcData = original.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData dstData = recolored.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* srcPtr = (byte*)srcData.Scan0;
                byte* dstPtr = (byte*)dstData.Scan0;
                int bytesPerPixel = 4;

                for (int y = 0; y < original.Height; y++)
                {
                    for (int x = 0; x < original.Width; x++)
                    {
                        int idx = y * srcData.Stride + x * bytesPerPixel;
                        byte a = srcPtr[idx + 3];

                        if (a <= 5)
                        {
                            dstPtr[idx] = 0;
                            dstPtr[idx + 1] = 0;
                            dstPtr[idx + 2] = 0;
                            dstPtr[idx + 3] = 0;
                            continue;
                        }

                        float alphaFactor = a / 255f;
                        dstPtr[idx] = (byte)(solidColor.B * alphaFactor);
                        dstPtr[idx + 1] = (byte)(solidColor.G * alphaFactor);
                        dstPtr[idx + 2] = (byte)(solidColor.R * alphaFactor);
                        dstPtr[idx + 3] = a;
                    }
                }
            }

            original.UnlockBits(srcData);
            recolored.UnlockBits(dstData);
            return recolored;
        }

        private void UpdateSpritePreviewFromBitmap(Bitmap sheetBmp)
        {
            if (SpriteSelector.SelectedItem == null) { SpritePreview.Source = null; return; }

            string sel = SpriteSelector.SelectedItem.ToString()!;
            var def = _previewSprites.FirstOrDefault(s => string.Equals(s.Name, sel, StringComparison.OrdinalIgnoreCase));
            if (def == null) { SpritePreview.Source = null; return; }

            using (var cropped = new Bitmap(def.W, def.H, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(cropped))
                {
                    g.DrawImage(sheetBmp, new Rectangle(0, 0, def.W, def.H), new Rectangle(def.X, def.Y, def.W, def.H), GraphicsUnit.Pixel);
                }

                SpritePreview.Source = BitmapToImageSource(cropped);
            }
        }

        private BitmapImage BitmapToImageSource(Bitmap bmp)
        {
            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            ms.Position = 0;
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.StreamSource = ms;
            bi.EndInit();
            bi.Freeze();
            return bi;
        }

        private void OnSpriteSelectorChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SheetPreview.Source is BitmapImage bi)
            {
                _ = UpdatePreviewAsync();
            }
            else
            {
                _ = UpdatePreviewAsync();
            }
        }
    }
}