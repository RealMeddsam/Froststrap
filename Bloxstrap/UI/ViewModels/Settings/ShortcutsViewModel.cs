using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public partial class ShortcutsViewModel : INotifyPropertyChanged
    {
        public ShortcutTask DesktopIconTask { get; } = new("Desktop", Paths.Desktop, $"{App.ProjectName}.lnk");
        public ShortcutTask StartMenuIconTask { get; } = new("StartMenu", Paths.WindowsStartMenu, $"{App.ProjectName}.lnk");
        public ShortcutTask PlayerIconTask { get; } = new("RobloxPlayer", Paths.Desktop, $"{Strings.LaunchMenu_LaunchRoblox}.lnk", "-player");
        public ShortcutTask StudioIconTask { get; } = new("RobloxStudio", Paths.Desktop, $"{Strings.LaunchMenu_LaunchRobloxStudio}.lnk", "-studio");
        public ShortcutTask SettingsIconTask { get; } = new("Settings", Paths.Desktop, $"{Strings.Menu_Title}.lnk", "-settings");
        public ExtractIconsTask ExtractIconsTask { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        public ObservableCollection<GameShortcut> GameShortcuts { get; } = new();
        public ObservableCollection<GameSearchResult> SearchResults { get; } = new();

        private GameShortcut? _selectedShortcut;
        public GameShortcut? SelectedShortcut
        {
            get => _selectedShortcut;
            set
            {
                if (_selectedShortcut != value)
                {
                    _selectedShortcut = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsShortcutSelected));
                    GameShortcutStatus = "";

                    if (value != null && !string.IsNullOrEmpty(value.GameId))
                    {
                        SearchQuery = value.GameId;
                    }
                    else
                    {
                        ClearSearch();
                    }
                }
            }
        }

        private GameSearchResult? _selectedSearchResult;
        public GameSearchResult? SelectedSearchResult
        {
            get => _selectedSearchResult;
            set
            {
                if (_selectedSearchResult != value)
                {
                    _selectedSearchResult = value;
                    OnPropertyChanged();

                    if (value != null && SelectedShortcut != null)
                    {
                        SelectedShortcut.GameId = value.RootPlaceId.ToString();
                        SelectedShortcut.GameName = value.Name;
                        SearchQuery = value.Name;
                        _searchDebounceCts?.Cancel();
                        _ = DownloadIconAsync();

                        OnPropertyChanged(nameof(SelectedShortcut));
                    }
                }
            }
        }

        private string _searchQuery = "";
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (_searchQuery != value)
                {
                    _searchQuery = value;
                    OnPropertyChanged();
                    OnSearchQueryChanged(value);
                }
            }
        }

        private string _gameShortcutStatus = "";
        public string GameShortcutStatus
        {
            get => _gameShortcutStatus;
            set
            {
                if (_gameShortcutStatus != value)
                {
                    _gameShortcutStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsShortcutSelected => SelectedShortcut != null;

        public RelayCommand AddShortcutCommand { get; }
        public RelayCommand RemoveShortcutCommand { get; }
        public RelayCommand CreateShortcutCommand { get; }
        public RelayCommand ClearSearchCommand { get; }

        private CancellationTokenSource? _searchDebounceCts;

        public ShortcutsViewModel()
        {
            AddShortcutCommand = new RelayCommand(AddShortcut);
            RemoveShortcutCommand = new RelayCommand(RemoveShortcut);
            CreateShortcutCommand = new RelayCommand(CreateShortcut);
            ClearSearchCommand = new RelayCommand(ClearSearch);

            LoadShortcuts();
        }

        private void AddShortcut()
        {
            var shortcut = new GameShortcut { GameName = "New Game", GameId = "" };
            GameShortcuts.Add(shortcut);
            SelectedShortcut = shortcut;
            SaveShortcuts();
        }

        private void RemoveShortcut()
        {
            if (SelectedShortcut == null) return;

            GameShortcuts.Remove(SelectedShortcut);
            SelectedShortcut = null;
            SaveShortcuts();
            CleanupUnusedIcons();
        }

        private void LoadShortcuts()
        {
            try
            {
                string json = App.Settings.Prop.GameShortcutsJson;
                var shortcuts = JsonSerializer.Deserialize<GameShortcut[]>(json) ?? Array.Empty<GameShortcut>();

                foreach (var shortcut in shortcuts)
                    GameShortcuts.Add(shortcut);

                if (GameShortcuts.Count == 0)
                    AddShortcut();
            }
            catch
            {
                AddShortcut();
            }
        }

        private void SaveShortcuts()
        {
            try
            {
                string json = JsonSerializer.Serialize(GameShortcuts.ToArray());
                App.Settings.Prop.GameShortcutsJson = json;
                App.Settings.Save();
            }
            catch { }
        }

        private async void OnSearchQueryChanged(string value)
        {
            _searchDebounceCts?.Cancel();
            _searchDebounceCts = new CancellationTokenSource();

            if (string.IsNullOrWhiteSpace(value))
            {
                SearchResults.Clear();
                return;
            }

            await DebouncedSearchAsync(_searchDebounceCts.Token);
        }

        private async Task DebouncedSearchAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(600, token);
                if (token.IsCancellationRequested) return;

                await SearchGamesAsync();

                if (SelectedShortcut != null && ulong.TryParse(SearchQuery, out ulong gameId))
                {
                    var matchingGame = SearchResults.FirstOrDefault(r => r.RootPlaceId == (long)gameId);
                    if (matchingGame != null)
                    {
                        SelectedShortcut.GameId = gameId.ToString();
                        SelectedShortcut.GameName = matchingGame.Name;
                        _ = DownloadIconAsync();
                        OnPropertyChanged(nameof(SelectedShortcut));
                    }
                }
            }
            catch (TaskCanceledException) { }
        }

        private async Task SearchGamesAsync()
        {
            try
            {
                var results = await GameSearching.GetGameSearchResultsAsync(SearchQuery);

                SearchResults.Clear();

                foreach (var result in results.Take(5))
                    SearchResults.Add(result);
            }
            catch { }
        }

        private void ClearSearch()
        {
            SearchQuery = "";
            SearchResults.Clear();
            SelectedSearchResult = null;
        }

        private async Task DownloadIconAsync()
        {
            if (SelectedShortcut == null || !ulong.TryParse(SelectedShortcut.GameId, out ulong gameId))
            {
                GameShortcutStatus = "Invalid Game ID";
                return;
            }

            try
            {
                var request = new ThumbnailRequest
                {
                    TargetId = gameId,
                    Type = "PlaceIcon",
                    Size = "128x128",
                    Format = "Png"
                };

                string? url = await Thumbnails.GetThumbnailUrlAsync(request, CancellationToken.None);
                if (string.IsNullOrEmpty(url))
                {
                    GameShortcutStatus = "No icon found";
                    return;
                }

                using var http = new HttpClient();
                var imageBytes = await http.GetByteArrayAsync(url);

                string hash = ComputeHash(imageBytes);
                string shortcutsIconDir = Path.Combine(Paths.Cache, "Game Shortcuts");
                Directory.CreateDirectory(shortcutsIconDir);

                string iconPath = Path.Combine(shortcutsIconDir, $"{hash}.ico");

                if (!File.Exists(iconPath))
                {
                    using var stream = new MemoryStream(imageBytes);
                    using var bitmap = new Bitmap(stream);
                    using var file = File.Create(iconPath);
                    SaveBitmapAsIcon(bitmap, file);
                }

                SelectedShortcut.IconPath = iconPath;
                SaveShortcuts();

                OnPropertyChanged(nameof(SelectedShortcut));
            }
            catch (Exception ex)
            {
                GameShortcutStatus = $"Error: {ex.Message}";
            }
        }

        private void CreateShortcut()
        {
            if (SelectedShortcut == null || string.IsNullOrWhiteSpace(SelectedShortcut.GameId))
            {
                GameShortcutStatus = "Game ID required";
                return;
            }

            try
            {
                string url = $"roblox://placeId={SelectedShortcut.GameId}/";
                string safeName = SanitizeFileName(SelectedShortcut.GameName);
                string shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{safeName}.url");

                if (File.Exists(shortcutPath))
                    File.Delete(shortcutPath);

                using var writer = new StreamWriter(shortcutPath);
                writer.WriteLine("[InternetShortcut]");
                writer.WriteLine($"URL={url}");

                if (!string.IsNullOrEmpty(SelectedShortcut.IconPath) && File.Exists(SelectedShortcut.IconPath))
                {
                    writer.WriteLine($"IconFile={SelectedShortcut.IconPath}");
                    writer.WriteLine("IconIndex=0");
                }

                GameShortcutStatus = "Shortcut created on desktop";
            }
            catch (Exception ex)
            {
                GameShortcutStatus = $"Error: {ex.Message}";
            }
        }

        private static void SaveBitmapAsIcon(Bitmap bitmap, Stream output)
        {
            using var resized = new Bitmap(bitmap, new Size(64, 64));
            using var iconBitmap = new Bitmap(64, 64, PixelFormat.Format32bppArgb);

            using (var graphics = Graphics.FromImage(iconBitmap))
                graphics.DrawImage(resized, 0, 0, 64, 64);

            using var stream = new MemoryStream();
            iconBitmap.Save(stream, ImageFormat.Png);
            var pngBytes = stream.ToArray();

            using var writer = new BinaryWriter(output);
            writer.Write((short)0);
            writer.Write((short)1);
            writer.Write((short)1);

            writer.Write((byte)64);
            writer.Write((byte)64);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((short)1);
            writer.Write((short)32);
            writer.Write(pngBytes.Length);
            writer.Write(22);

            writer.Write(pngBytes);
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Trim();
        }

        private static string ComputeHash(byte[] data)
        {
            using var sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(data);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private void CleanupUnusedIcons()
        {
            try
            {
                var usedIcons = GameShortcuts
                    .Where(s => !string.IsNullOrEmpty(s.IconPath))
                    .Select(s => Path.GetFullPath(s.IconPath))
                    .ToHashSet();

                string shortcutsIconDir = Path.Combine(Paths.Cache, "Game Shortcuts");
                if (Directory.Exists(shortcutsIconDir))
                {
                    foreach (string iconFile in Directory.GetFiles(shortcutsIconDir, "*.ico"))
                    {
                        if (!usedIcons.Contains(Path.GetFullPath(iconFile)))
                            File.Delete(iconFile);
                    }
                }
            }
            catch { }
        }
    }

    public class GameShortcut : INotifyPropertyChanged
    {
        private string _gameName = "";
        private string _gameId = "";
        private string _iconPath = "";

        public string GameName
        {
            get => _gameName;
            set
            {
                if (_gameName != value)
                {
                    _gameName = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GameName)));
                }
            }
        }

        public string GameId
        {
            get => _gameId;
            set
            {
                if (_gameId != value)
                {
                    _gameId = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GameId)));
                }
            }
        }

        public string IconPath
        {
            get => _iconPath;
            set
            {
                if (_iconPath != value)
                {
                    _iconPath = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IconPath)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}