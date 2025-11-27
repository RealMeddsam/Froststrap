using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Compression;
using System.Reflection;

namespace Bloxstrap.Integrations
{
    public static class ModGenerator
    {
        public static void RecolorAllPngs(string rootDir, Color solidColor, bool recolorCursors = false, bool recolorShiftlock = false, bool recolorEmoteWheel = false, bool recolorVoiceChat = false)
        {
            const string LOG_IDENT = "UI::Recolor";

            if (string.IsNullOrWhiteSpace(rootDir) || !Directory.Exists(rootDir))
            {
                App.Logger?.WriteLine(LOG_IDENT, $"Invalid rootDir '{rootDir}'");
                return;
            }

            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("Bloxstrap.Resources.mappings.json");
            if (stream == null)
            {
                App.Logger?.WriteLine(LOG_IDENT, "mappings.json embedded resource not found");
                return;
            }

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            var mappings = JsonSerializer.Deserialize<Dictionary<string, string[]>>(json);
            if (mappings == null || mappings.Count == 0)
            {
                App.Logger?.WriteLine(LOG_IDENT, "mappings.json parsed but empty");
                return;
            }

            App.Logger?.WriteLine(LOG_IDENT, $"Loaded {mappings.Count} valid entries from mappings.json");
            App.Logger?.WriteLine(LOG_IDENT, "RecolorAllPngs started.");

            foreach (var kv in mappings)
            {
                string[] parts = kv.Value;
                string relativePath = Path.Combine(parts);
                string fullPath = Path.Combine(rootDir, relativePath);

                if (!File.Exists(fullPath))
                {
                    App.Logger?.WriteLine(LOG_IDENT, $"File missing: {relativePath}");
                    continue;
                }

                try
                {
                    App.Logger?.WriteLine(LOG_IDENT, $"Recoloring '{relativePath}'");
                    SafeRecolorImage(fullPath, solidColor);
                }
                catch (Exception ex)
                {
                    App.Logger?.WriteLine(LOG_IDENT, $"Error recoloring '{relativePath}': {ex.Message}");
                }
            }

            // Handle additional files based on options
            void TryRecolorFilesByNames(string[] candidateNames, string? relativeDir = null)
            {
                foreach (var name in candidateNames)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(relativeDir))
                        {
                            string expected = Path.Combine(rootDir, relativeDir, name);
                            if (File.Exists(expected))
                            {
                                SafeRecolorImage(expected, solidColor);
                                continue;
                            }
                        }

                        var matches = Directory.EnumerateFiles(rootDir, name, SearchOption.AllDirectories).ToList();
                        if (matches.Count == 0)
                        {
                            matches = Directory.EnumerateFiles(rootDir, "*.png", SearchOption.AllDirectories)
                                .Where(p => p.EndsWith(name, StringComparison.OrdinalIgnoreCase)).ToList();
                        }

                        if (matches.Count > 0)
                        {
                            foreach (var m in matches)
                            {
                                try
                                {
                                    SafeRecolorImage(m, solidColor);
                                }
                                catch (Exception ex)
                                {
                                    App.Logger?.WriteLine(LOG_IDENT, $"Error recoloring matched file '{m}': {ex.Message}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        App.Logger?.WriteLine(LOG_IDENT, $"Exception while trying to handle '{name}': {ex.Message}");
                    }
                }
            }

            if (recolorCursors)
                TryRecolorFilesByNames(
                    new[] { "IBeamCursor.png", "ArrowCursor.png", "ArrowFarCursor.png" },
                    Path.Combine("content", "textures", "Cursors", "KeyboardMouse"));

            if (recolorShiftlock)
                TryRecolorFilesByNames(
                    new[] { "MouseLockedCursor.png" },
                    Path.Combine("content", "textures"));

            if (recolorEmoteWheel)
                TryRecolorFilesByNames(
                    new[]
                    {
                        "SelectedGradient.png", "SelectedGradient@2x.png", "SelectedGradient@3x.png",
                        "SelectedLine.png", "SelectedLine@2x.png", "SelectedLine@3x.png"
                    },
                    Path.Combine("content", "textures", "ui", "Emotes", "Large"));

            App.Logger?.WriteLine(LOG_IDENT, "RecolorAllPngs finished.");
        }

        private static void SafeRecolorImage(string path, Color solidColor)
        {
            using (var original = new Bitmap(path))
            using (var recolored = ApplyMask(original, solidColor))
            {
                string tempPath = path + ".tmp";
                recolored.Save(tempPath, ImageFormat.Png);
            }

            ReplaceFileWithRetry(path, path + ".tmp");
        }

        private static Bitmap ApplyMask(Bitmap original, Color solidColor)
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

                        if (a == 0)
                        {
                            dstPtr[idx] = 0;
                            dstPtr[idx + 1] = 0;
                            dstPtr[idx + 2] = 0;
                            dstPtr[idx + 3] = 0;
                            continue;
                        }

                        dstPtr[idx] = solidColor.B;
                        dstPtr[idx + 1] = solidColor.G;
                        dstPtr[idx + 2] = solidColor.R;
                        dstPtr[idx + 3] = a;
                    }
                }
            }

            original.UnlockBits(srcData);
            recolored.UnlockBits(dstData);
            return recolored;
        }

        private static void ReplaceFileWithRetry(string originalPath, string tempPath)
        {
            int attempts = 0;
            while (true)
            {
                try
                {
                    File.Delete(originalPath);
                    File.Move(tempPath, originalPath);
                    break;
                }
                catch (IOException)
                {
                    attempts++;
                    if (attempts > 5)
                        throw;
                    Thread.Sleep(50);
                }
            }
        }

        public static void ZipResult(string sourceDir, string outputZip)
        {
            if (File.Exists(outputZip))
                File.Delete(outputZip);

            ZipFile.CreateFromDirectory(sourceDir, outputZip, CompressionLevel.Optimal, false);
        }
    }
}