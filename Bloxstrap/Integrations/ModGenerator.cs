using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO.Compression;

namespace Bloxstrap.Integrations
{
    public static class ModGenerator
    {
        private static readonly SpriteBlacklist SpriteBlacklistInstance = new SpriteBlacklist
        {
            Prefixes = new List<string>
            {
                "chat_bubble/",
                "component_assets/",
                "icons/controls/voice/",
                "icons/graphic/",
                "squircles/"
            },
            Suffixes = new List<string>(),
            Keywords = new List<string>
            {
                "goldrobux",
                "logo/studiologo"
            },
            Strict = new List<string>
            {
                "gradient/gradient_0_100"
            }
        };

        public class SpriteBlacklist
        {
            public List<string> Prefixes { get; set; } = new();
            public List<string> Suffixes { get; set; } = new();
            public List<string> Keywords { get; set; } = new();
            public List<string> Strict { get; set; } = new();

            public bool IsBlacklisted(string name)
            {
                foreach (var p in Prefixes)
                    if (name.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                        return true;
                foreach (var s in Suffixes)
                    if (name.EndsWith(s, StringComparison.OrdinalIgnoreCase))
                        return true;
                foreach (var k in Keywords)
                    if (name.Contains(k, StringComparison.OrdinalIgnoreCase))
                        return true;
                foreach (var str in Strict)
                    if (string.Equals(name, str, StringComparison.OrdinalIgnoreCase))
                        return true;
                return false;
            }
        }

        public record GradientStop(float Stop, Color Color);

        public static void RecolorAllPngs(string rootDir, Color? solidColor, List<GradientStop>? gradient = null, string getImageSetDataPath = "", string? customLogoPath = null, float gradientAngleDeg = 0f, bool recolorCursors = false, bool recolorShiftlock = false, bool recolorEmoteWheel = false, IEnumerable<string>? extraSourceDirs = null)
        {
            const string LOG_IDENT = "UI::Recolor";

            if (string.IsNullOrWhiteSpace(rootDir) || !Directory.Exists(rootDir))
            {
                App.Logger?.WriteLine(LOG_IDENT, $"Invalid rootDir '{rootDir}'");
                return;
            }

            App.Logger?.WriteLine(LOG_IDENT, "RecolorAllPngs started.");

            foreach (var png in Directory.EnumerateFiles(rootDir, "*.png", SearchOption.AllDirectories))
            {
                if (png.Contains(@"\SpriteSheets\", StringComparison.OrdinalIgnoreCase))
                    continue;

                SafeRecolorImage(png, solidColor, gradient, gradientAngleDeg);
            }

            if (!string.IsNullOrWhiteSpace(getImageSetDataPath) && File.Exists(getImageSetDataPath))
            {
                App.Logger?.WriteLine(LOG_IDENT, $"Parsing image set data: {getImageSetDataPath}");

                var spriteData = LuaImageSetParser.Parse(getImageSetDataPath);
                foreach (var (sheetPath, sprites) in spriteData)
                {
                    if (!File.Exists(sheetPath)) continue;
                    SafeRecolorSpriteSheet(sheetPath, sprites, solidColor, gradient, gradientAngleDeg);
                }
            }

            if (!string.IsNullOrEmpty(customLogoPath) && File.Exists(getImageSetDataPath))
            {
                App.Logger?.WriteLine(LOG_IDENT, $"Applying custom logo: {customLogoPath}");

                var spriteData = LuaImageSetParser.Parse(getImageSetDataPath);
                foreach (var (sheetPath, sprites) in spriteData)
                {
                    if (!File.Exists(sheetPath)) continue;

                    bool modified = false;
                    string tempPath = sheetPath + ".logo.tmp";
                    using Bitmap customInMemory = LoadBitmapIntoMemory(customLogoPath);

                    using (var sheet = new Bitmap(sheetPath))
                    using (var g = Graphics.FromImage(sheet))
                    {
                        g.CompositingMode = CompositingMode.SourceOver;
                        g.CompositingQuality = CompositingQuality.HighQuality;
                        g.SmoothingMode = SmoothingMode.HighQuality;

                        foreach (var sprite in sprites)
                        {
                            if (!string.Equals(sprite.Name, "icons/logo/block", StringComparison.OrdinalIgnoreCase))
                                continue;
                            if (sprite.W <= 0 || sprite.H <= 0) continue;

                            Rectangle targetRect = new Rectangle(sprite.X, sprite.Y, sprite.W, sprite.H);

                            var prevCompositing = g.CompositingMode;
                            g.CompositingMode = CompositingMode.SourceCopy;
                            using (var clearBrush = new SolidBrush(Color.FromArgb(0, 0, 0, 0)))
                            {
                                g.FillRectangle(clearBrush, targetRect);
                            }
                            g.CompositingMode = prevCompositing;

                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.CompositingMode = CompositingMode.SourceOver;
                            g.DrawImage(customInMemory, targetRect);

                            modified = true;
                        }

                        if (modified) sheet.Save(tempPath, ImageFormat.Png);
                    }

                    if (modified)
                    {
                        ReplaceFileWithRetry(sheetPath, tempPath);
                        App.Logger?.WriteLine(LOG_IDENT, $"Replaced logo in {sheetPath}");
                    }
                }
            }

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
                                SafeRecolorImage(expected, solidColor, gradient, gradientAngleDeg);
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
                                    SafeRecolorImage(m, solidColor, gradient, gradientAngleDeg);
                                }
                                catch (Exception ex)
                                {
                                    App.Logger?.WriteLine(LOG_IDENT, $"Error recoloring matched file '{m}': {ex.Message}");
                                }
                            }
                            continue;
                        }

                        if (extraSourceDirs != null)
                        {
                            foreach (var srcBase in extraSourceDirs)
                            {
                                if (string.IsNullOrWhiteSpace(srcBase) || !Directory.Exists(srcBase)) continue;

                                var srcMatches = Directory.EnumerateFiles(srcBase, name, SearchOption.AllDirectories).ToList();
                                if (srcMatches.Count == 0)
                                {
                                    srcMatches = Directory.EnumerateFiles(srcBase, "*.png", SearchOption.AllDirectories)
                                        .Where(p => p.EndsWith(name, StringComparison.OrdinalIgnoreCase)).ToList();
                                }

                                foreach (var src in srcMatches)
                                {
                                    try
                                    {
                                        string destDir;
                                        if (!string.IsNullOrWhiteSpace(relativeDir))
                                        {
                                            destDir = Path.Combine(rootDir, relativeDir);
                                        }
                                        else
                                        {
                                            var rel = Path.GetRelativePath(srcBase, Path.GetDirectoryName(src) ?? string.Empty);
                                            destDir = Path.Combine(rootDir, rel);
                                        }

                                        Directory.CreateDirectory(destDir);
                                        string destPath = Path.Combine(destDir, name);

                                        File.Copy(src, destPath, overwrite: true);
                                        SafeRecolorImage(destPath, solidColor, gradient, gradientAngleDeg);

                                        App.Logger?.WriteLine(LOG_IDENT, $"Copied + recolored '{src}' → '{destPath}'");
                                    }
                                    catch (Exception ex)
                                    {
                                        App.Logger?.WriteLine(LOG_IDENT, $"Failed copying/recoloring from extraSourceDirs '{src}': {ex.Message}");
                                    }
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
                        "SelectedLine.png", "SelectedLine@2x.png", "SelectedLine@3x.png",
                        "SegmentedCircle.png", "SegmentedCircle@2x.png", "SegmentedCircle@3x.png"
                    },
                    Path.Combine("content", "textures", "ui", "Emotes", "Large"));

            App.Logger?.WriteLine(LOG_IDENT, "RecolorAllPngs finished.");
        }

        private static Bitmap LoadBitmapIntoMemory(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var bmpFromStream = new Bitmap(fs);
                var copy = new Bitmap(bmpFromStream.Width, bmpFromStream.Height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(copy))
                {
                    g.CompositingMode = CompositingMode.SourceOver;
                    g.DrawImage(bmpFromStream, new Rectangle(0, 0, copy.Width, copy.Height));
                }
                return copy;
            }
        }

        public static void ZipResult(string sourceDir, string outputZip)
        {
            if (File.Exists(outputZip))
                File.Delete(outputZip);

            ZipFile.CreateFromDirectory(sourceDir, outputZip, CompressionLevel.Optimal, false);
        }

        private static void SafeRecolorImage(string path, Color? solidColor, List<GradientStop>? gradient, float gradientAngleDeg)
        {
            using (var original = new Bitmap(path))
            using (var recolored = ApplyMask(original, solidColor, gradient, gradientAngleDeg))
            {
                string tempPath = path + ".tmp";
                recolored.Save(tempPath, ImageFormat.Png);
            }

            ReplaceFileWithRetry(path, path + ".tmp");
        }

        private static void SafeRecolorSpriteSheet(string sheetPath, List<SpriteDef> sprites, Color? solidColor, List<GradientStop>? gradient, float gradientAngleDeg)
        {
            string tempPath = sheetPath + ".tmp";

            using (var sheet = new Bitmap(sheetPath))
            using (var output = new Bitmap(sheet.Width, sheet.Height, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(output))
            {
                g.Clear(Color.Transparent);

                foreach (var sprite in sprites)
                {
                    if (sprite.W <= 0 || sprite.H <= 0)
                        continue;

                    if (SpriteBlacklistInstance.IsBlacklisted(sprite.Name))
                    {
                        Rectangle rect = new Rectangle(sprite.X, sprite.Y, sprite.W, sprite.H);
                        using var croppedOriginal = sheet.Clone(rect, sheet.PixelFormat);
                        g.DrawImage(croppedOriginal, rect);
                        continue;
                    }

                    Rectangle r = new Rectangle(sprite.X, sprite.Y, sprite.W, sprite.H);
                    using var cropped = sheet.Clone(r, sheet.PixelFormat);
                    using var recolored = ApplyMask(cropped, solidColor, gradient, gradientAngleDeg);
                    g.DrawImage(recolored, r);
                }

                output.Save(tempPath, ImageFormat.Png);
            }

            ReplaceFileWithRetry(sheetPath, tempPath);
        }

        private static Bitmap ApplyMask(Bitmap original, Color? solidColor, List<GradientStop>? gradient, float gradientAngleDeg)
        {
            if (original.Width == 0 || original.Height == 0)
                return new Bitmap(original);

            var recolored = new Bitmap(original.Width, original.Height, PixelFormat.Format32bppArgb);
            var rect = new Rectangle(0, 0, original.Width, original.Height);
            BitmapData srcData = original.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData dstData = recolored.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            // Angle math
            double theta = gradientAngleDeg * Math.PI / 180.0;
            double cos = Math.Cos(theta);
            double sin = Math.Sin(theta);

            double w = original.Width - 1;
            double h = original.Height - 1;
            double[] projs = new double[]
            {
        0 * cos + 0 * sin,
        w * cos + 0 * sin,
        0 * cos + h * sin,
        w * cos + h * sin
            };
            double minProj = projs.Min();
            double maxProj = projs.Max();
            double denom = maxProj - minProj;
            if (Math.Abs(denom) < 1e-6) denom = 1.0;

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

                        Color overlay;
                        if (gradient != null && gradient.Count > 0)
                        {
                            double proj = x * cos + y * sin;
                            float t = (float)((proj - minProj) / denom);
                            t = Math.Clamp(t, 0f, 1f);
                            overlay = InterpolateGradient(gradient, t);
                        }
                        else
                        {
                            overlay = solidColor ?? Color.White;
                        }

                        dstPtr[idx] = overlay.B;
                        dstPtr[idx + 1] = overlay.G;
                        dstPtr[idx + 2] = overlay.R;
                        dstPtr[idx + 3] = a;
                    }
                }
            }

            original.UnlockBits(srcData);
            recolored.UnlockBits(dstData);
            return recolored;
        }

        private static Color InterpolateGradient(List<GradientStop> gradient, float t)
        {
            t = Math.Clamp(t, 0f, 1f);

            GradientStop left = gradient[0];
            GradientStop right = gradient[^1];

            for (int i = 0; i < gradient.Count - 1; i++)
            {
                if (t >= gradient[i].Stop && t <= gradient[i + 1].Stop)
                {
                    left = gradient[i];
                    right = gradient[i + 1];
                    break;
                }
            }

            float span = right.Stop - left.Stop;
            float localT = span > 0f ? (t - left.Stop) / span : 0f;

            int r = Math.Clamp((int)(left.Color.R + (right.Color.R - left.Color.R) * localT), 0, 255);
            int g = Math.Clamp((int)(left.Color.G + (right.Color.G - left.Color.G) * localT), 0, 255);
            int b = Math.Clamp((int)(left.Color.B + (right.Color.B - left.Color.B) * localT), 0, 255);
            int a = Math.Clamp((int)(left.Color.A + (right.Color.A - left.Color.A) * localT), 0, 255);

            return Color.FromArgb(a, r, g, b);
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

        public record SpriteDef(string Name, int X, int Y, int W, int H);

        private static class LuaImageSetParser
        {
            public static Dictionary<string, List<SpriteDef>> Parse(string luaPath)
            {
                string text = File.ReadAllText(luaPath);
                var result = new Dictionary<string, List<SpriteDef>>();

                var regex = new Regex(@"\['([^']+)'\]\s*=\s*{[^}]*ImageRectOffset\s*=\s*Vector2\.new\((\d+),\s*(\d+)\)[^}]*ImageRectSize\s*=\s*Vector2\.new\((\d+),\s*(\d+)\)[^}]*ImageSet\s*=\s*'([^']+)'", RegexOptions.Compiled);

                foreach (Match match in regex.Matches(text))
                {
                    string name = match.Groups[1].Value;
                    int x = int.Parse(match.Groups[2].Value);
                    int y = int.Parse(match.Groups[3].Value);
                    int w = int.Parse(match.Groups[4].Value);
                    int h = int.Parse(match.Groups[5].Value);
                    string imageSet = match.Groups[6].Value + ".png";

                    string imagePath = Path.Combine(Path.GetDirectoryName(luaPath)!, @"..\SpriteSheets", imageSet);
                    imagePath = Path.GetFullPath(imagePath);

                    if (!result.ContainsKey(imagePath))
                        result[imagePath] = new List<SpriteDef>();

                    result[imagePath].Add(new SpriteDef(name, x, y, w, h));
                }

                return result;
            }
        }
    }
}