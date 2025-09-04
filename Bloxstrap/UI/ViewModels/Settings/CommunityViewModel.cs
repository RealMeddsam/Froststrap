using Bloxstrap.Integrations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class FlaglistEntry : ObservableObject
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = "";

        private string _title = "";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string? Name { get; set; }
        public string DisplayName => string.IsNullOrWhiteSpace(Name) ? "Unknown" : Name;
        public string AuthorText => $"Author: {DisplayName}";

        private string _json = "";
        public string Json
        {
            get => _json;
            set
            {
                SetProperty(ref _json, value);
            }
        }

        public DateTime CreatedAt { get; set; }

        private DateTime _updatedAt;
        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set => SetProperty(ref _updatedAt, value);
        }

        public string FormattedJson
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Json))
                    return "// No JSON loaded";

                try
                {
                    using var doc = JsonDocument.Parse(Json);
                    return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                }
                catch
                {
                    return Json;
                }
            }
        }

        public int FlagCount
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FormattedJson))
                    return 0;

                int count = FormattedJson
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Length;

                return Math.Max(0, count - 2);
            }
        }
    }

    public class CommunityViewModel : ObservableObject
    {
        private readonly SupabaseService _supabaseService;
        private readonly DispatcherTimer _debounceTimer;

        private string _title = "";
        private string _json = "";
        private string _statusMessage = "";
        private string _searchQuery = "";
        private string _editableTitle = "";
        private string _editableJson = "";
        private int _loadedCount = 0;
        private bool _isUploading = false;
        private bool _isLoadingMore = false;
        private bool _hasMoreToLoad = true;
        private bool _isNoResultsFound = false;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Json
        {
            get => _json;
            set => SetProperty(ref _json, value);
        }

        private string? _name;
        public string? Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string EditableTitle
        {
            get => _editableTitle;
            set => SetProperty(ref _editableTitle, value);
        }

        public string EditableJson
        {
            get => _editableJson;
            set => SetProperty(ref _editableJson, value);
        }

        private string _editableName = "";
        public string EditableName
        {
            get => _editableName;
            set => SetProperty(ref _editableName, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    _debounceTimer.Stop();
                    _debounceTimer.Start();
                }
            }
        }

        public bool IsUploading
        {
            get => _isUploading;
            private set
            {
                SetProperty(ref _isUploading, value);
                OnPropertyChanged(nameof(CanUpload));
            }
        }

        public bool CanUpload => !IsUploading;

        public bool HasMoreToLoad
        {
            get => _hasMoreToLoad;
            set
            {
                if (SetProperty(ref _hasMoreToLoad, value))
                    OnPropertyChanged(nameof(ShowLoadingMoreText));
            }
        }

        public bool IsLoadingMore
        {
            get => _isLoadingMore;
            set
            {
                if (SetProperty(ref _isLoadingMore, value))
                    OnPropertyChanged(nameof(ShowLoadingMoreText));
            }
        }

        public bool ShowLoadingMoreText => IsLoadingMore && HasMoreToLoad;

        public bool IsNoResultsFound
        {
            get => _isNoResultsFound;
            private set => SetProperty(ref _isNoResultsFound, value);
        }

        private FlaglistEntry? _selectedFlaglist;
        public FlaglistEntry? SelectedFlaglist
        {
            get => _selectedFlaglist;
            set
            {
                if (SetProperty(ref _selectedFlaglist, value))
                {
                    UpdateEditableFromSelection();
                    ApplyBulkUpdateCommand.NotifyCanExecuteChanged();
                    DeleteSelectedFlaglistsCommand.NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(IsAnyFlaglistSelected));
                }
            }
        }

        private string _userId = App.Settings.Prop.UserId ?? "";
        public string UserId
        {
            get => _userId;
            set
            {
                if (SetProperty(ref _userId, value))
                {
                    HasUserIdChanged = _userId != (App.Settings.Prop.UserId ?? "");
                }
            }
        }

        private bool _hasUserIdChanged;
        public bool HasUserIdChanged
        {
            get => _hasUserIdChanged;
            private set => SetProperty(ref _hasUserIdChanged, value);
        }

        public bool IsAnyFlaglistSelected => SelectedFlaglist != null;

        public ObservableCollection<FlaglistEntry> PublishedFlaglists { get; } = new();
        public ObservableCollection<FlaglistEntry> FilteredFlaglists { get; } = new();
        public ObservableCollection<FlaglistEntry> VisibleFlaglists { get; } = new();
        public ObservableCollection<FlaglistEntry> MyFlaglists { get; } = new();

        public RelayCommand SaveUserIdCommand => new(() =>
        {
            if (!Guid.TryParse(UserId, out _))
            {
                Frontend.ShowMessageBox(
                    "Only valid GUID type User IDs are allowed.",
                    MessageBoxImage.Warning,
                    MessageBoxButton.OK);
                return;
            }

            var result = Frontend.ShowMessageBox(
                "Are you sure you want to change your User ID? This will cause loss of access to your published flaglists.",
                MessageBoxImage.Warning,
                MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                App.Settings.Prop.UserId = UserId;
                App.Settings.Save();

                HasUserIdChanged = false;
            }
        });

        public ICommand UploadCommand { get; }
        public ICommand CopyJsonCommand { get; }
        public ICommand LoadMoreCommand { get; }
        public ICommand GetCurrentFlagsCommand { get; }
        public ICommand DeleteFlaglistCommand { get; }
        public ICommand UpdateFlaglistCommand { get; }
        public IAsyncRelayCommand ApplyBulkUpdateCommand { get; }
        public IAsyncRelayCommand DeleteSelectedFlaglistsCommand { get; }

        public CommunityViewModel()
        {
            _supabaseService = new SupabaseService(
                "Cant Share",
                "Cant Share"
            );

            UploadCommand = new RelayCommand(async () => await UploadAsync(), () => CanUpload);
            CopyJsonCommand = new RelayCommand<FlaglistEntry>(CopyJson!);
            LoadMoreCommand = new RelayCommand(LoadNextBatch);
            GetCurrentFlagsCommand = new RelayCommand(GetCurrentFlags);
            DeleteFlaglistCommand = new RelayCommand<FlaglistEntry>(async (entry) => await DeleteFlaglistAsync(entry!));
            UpdateFlaglistCommand = new RelayCommand<FlaglistEntry>(async (entry) => await UpdateFlaglistAsync(entry!));
            ApplyBulkUpdateCommand = new AsyncRelayCommand(ApplyBulkUpdateAsync, () => IsAnyFlaglistSelected);
            DeleteSelectedFlaglistsCommand = new AsyncRelayCommand(DeleteSelectedFlaglistsAsync, () => IsAnyFlaglistSelected);

            _debounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _debounceTimer.Tick += (_, _) =>
            {
                _debounceTimer.Stop();
                ApplySearchFilter();
            };

            MyFlaglists.CollectionChanged += (s, e) =>
            {
                if (!MyFlaglists.Contains(SelectedFlaglist!))
                    SelectedFlaglist = null;

                OnPropertyChanged(nameof(IsAnyFlaglistSelected));
                UpdateEditableFromSelection();

                ApplyBulkUpdateCommand.NotifyCanExecuteChanged();
                DeleteSelectedFlaglistsCommand.NotifyCanExecuteChanged();
            };

            _ = LoadPublishedFlaglistsAsync();
        }

        private async Task UploadAsync()
        {
            StatusMessage = "";

            if (string.IsNullOrWhiteSpace(Title))
            {
                StatusMessage = "Title cannot be empty.";
                return;
            }

            if (ContainsBannedWords(Title))
            {
                StatusMessage = "Title contains bad words.";
                return;
            }

            if (!string.IsNullOrWhiteSpace(EditableName) && ContainsBannedWords(EditableName))
            {
                StatusMessage = "Author name contains bad words.";
                return;
            }

            if (string.IsNullOrWhiteSpace(Json))
            {
                StatusMessage = "Flaglist JSON cannot be empty.";
                return;
            }

            try
            {
                using var _ = JsonDocument.Parse(Json);

                if (!IsMostlyFastFlags(Json))
                {
                    StatusMessage = "A lot of your FastFlags don't have real FFlag prefixes.";
                    return;
                }

                if (!IsWithinLineLimit(Json))
                {
                    StatusMessage = "Too many lines in flaglist. Max allowed is 1250.";
                    return;
                }
            }
            catch (JsonException ex)
            {
                var msg = ex.Message.Split('\n').FirstOrDefault()?.Trim();
                StatusMessage = $"Invalid JSON format: {msg}";
                return;
            }

            IsUploading = true;

            try
            {
                var success = await _supabaseService.UploadFlaglistAsync(Title.Trim(), Json.Trim(), App.Settings.Prop.UserId);

                if (success)
                {
                    StatusMessage = "Upload successful!";
                    Title = "";
                    Json = "";
                    await LoadPublishedFlaglistsAsync();
                }
                else
                {
                    StatusMessage = "Upload failed. Please try again.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Upload error: {ex.Message}";
            }
            finally
            {
                IsUploading = false;
            }
        }

        private async Task LoadPublishedFlaglistsAsync()
        {
            try
            {
                var flaglists = await _supabaseService.GetFlaglistsAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    PublishedFlaglists.Clear();
                    MyFlaglists.Clear();

                    var myFlaglists = flaglists
                        .Where(f => f.user_id == App.Settings.Prop.UserId)
                        .OrderByDescending(f => f.created_at);

                    foreach (var f in flaglists.OrderByDescending(f => f.created_at))
                    {
                        var entry = new FlaglistEntry
                        {
                            Id = f.id,
                            UserId = f.user_id ?? "",
                            Title = f.title ?? "",
                            Json = f.json.ToString(),
                            CreatedAt = f.created_at.ToLocalTime(),
                            UpdatedAt = (f as dynamic).updated_at != null
                                        ? ((DateTime)(f as dynamic).updated_at).ToLocalTime()
                                        : f.created_at.ToLocalTime(),
                            Name = (f as dynamic).name ?? ""
                        };
                        PublishedFlaglists.Add(entry);
                    }

                    foreach (var f in myFlaglists)
                    {
                        var entry = new FlaglistEntry
                        {
                            Id = f.id,
                            UserId = f.user_id ?? "",
                            Title = f.title ?? "",
                            Json = f.json.ToString(),
                            CreatedAt = f.created_at.ToLocalTime(),
                            UpdatedAt = (f as dynamic).updated_at != null
                                        ? ((DateTime)(f as dynamic).updated_at).ToLocalTime()
                                        : f.created_at.ToLocalTime(),
                            Name = (f as dynamic).name ?? ""
                        };
                        MyFlaglists.Add(entry);
                    }

                    SelectedFlaglist = MyFlaglists.FirstOrDefault();

                    UpdateEditableFromSelection();
                    ApplySearchFilter();
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load flaglists: {ex.Message}";
            }
        }

        private void UpdateEditableFromSelection()
        {
            var selected = SelectedFlaglist;
            if (selected != null)
            {
                EditableTitle = selected.Title;

                try
                {
                    using var doc = JsonDocument.Parse(selected.Json);
                    EditableJson = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
                }
                catch
                {
                    EditableJson = selected.Json;
                }

                EditableName = selected.Name ?? "";
            }
            else
            {
                EditableTitle = "";
                EditableJson = "";
                EditableName = "";
            }
        }

        private void ApplySearchFilter()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                FilteredFlaglists.Clear();
                VisibleFlaglists.Clear();
                _loadedCount = 0;

                var query = SearchQuery?.Trim().ToLowerInvariant();
                var filtered = string.IsNullOrWhiteSpace(query)
                    ? PublishedFlaglists
                    : PublishedFlaglists.Where(f => f.Title.ToLowerInvariant().Contains(query));

                foreach (var entry in filtered)
                    FilteredFlaglists.Add(entry);

                IsNoResultsFound = FilteredFlaglists.Count == 0;

                LoadNextBatch();
            });
        }

        private void LoadNextBatch()
        {
            if (_loadedCount >= FilteredFlaglists.Count)
            {
                HasMoreToLoad = false;
                return;
            }

            HasMoreToLoad = true;
            IsLoadingMore = true;

            const int batchSize = 15;

            var nextItems = FilteredFlaglists
                .Skip(_loadedCount)
                .Take(batchSize)
                .ToList();

            foreach (var item in nextItems)
                VisibleFlaglists.Add(item);

            _loadedCount += nextItems.Count;

            const int maxVisible = 30;
            while (VisibleFlaglists.Count > maxVisible)
                VisibleFlaglists.RemoveAt(0);

            IsLoadingMore = false;
        }

        private async Task DeleteFlaglistAsync(FlaglistEntry entry)
        {
            if (entry is null || entry.UserId != App.Settings.Prop.UserId)
                return;

            var success = await _supabaseService.DeleteFlaglistAsync(entry.Id, App.Settings.Prop.UserId);
            if (success)
                await LoadPublishedFlaglistsAsync();
        }

        private async Task UpdateFlaglistAsync(FlaglistEntry entry)
        {
            if (entry is null || entry.UserId != App.Settings.Prop.UserId)
                return;

            var success = await _supabaseService.UpdateFlaglistAsync(entry.Id, entry.Title, entry.Json, App.Settings.Prop.UserId);
            if (success)
                await LoadPublishedFlaglistsAsync();
        }

        private void CopyJson(FlaglistEntry entry)
        {
            if (entry is null)
                return;

            Clipboard.SetText(entry.Json);
        }

        private void GetCurrentFlags()
        {
            try
            {
                var flags = App.FastFlags.GetAllFlags();
                var dict = flags.ToDictionary(f => f.Name, f => f.Value);
                Json = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
                StatusMessage = "Current flags loaded.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to get current flags: {ex.Message}";
            }
        }

        private async Task ApplyBulkUpdateAsync()
        {
            StatusMessage = "";

            if (string.IsNullOrWhiteSpace(EditableTitle))
            {
                StatusMessage = "Title cannot be empty.";
                return;
            }

            if (ContainsBannedWords(EditableTitle))
            {
                StatusMessage = "Title contains bad words.";
                return;
            }

            if (!string.IsNullOrWhiteSpace(EditableName) && ContainsBannedWords(EditableName))
            {
                StatusMessage = "Author name contains bad words.";
                return;
            }

            if (string.IsNullOrWhiteSpace(EditableJson))
            {
                StatusMessage = "Flaglist JSON cannot be empty.";
                return;
            }

            try
            {
                using var _ = JsonDocument.Parse(EditableJson);

                if (!IsMostlyFastFlags(EditableJson))
                {
                    StatusMessage = "A lot of your FastFlags don't have real FFlag prefixes.";
                    return;
                }

                if (!IsWithinLineLimit(EditableJson))
                {
                    StatusMessage = "Too many lines in flaglist. Max allowed is 1250.";
                    return;
                }
            }
            catch (JsonException ex)
            {
                var msg = ex.Message.Split('\n').FirstOrDefault()?.Trim();
                StatusMessage = $"Invalid JSON format: {msg}";
                return;
            }

            var flaglist = SelectedFlaglist;
            if (flaglist == null)
            {
                StatusMessage = "No flaglist selected.";
                return;
            }

            var success = await _supabaseService.UpdateFlaglistAsync(flaglist.Id, EditableTitle, EditableJson, App.Settings.Prop.UserId, EditableName);

            if (success)
            {
                flaglist.Title = EditableTitle;
                flaglist.Json = EditableJson;
                flaglist.Name = EditableName;
                flaglist.UpdatedAt = DateTime.Now;
                StatusMessage = "Changes applied successfully.";
            }
            else
            {
                StatusMessage = $"Failed to update flaglist '{flaglist.Title}'.";
            }

            await LoadPublishedFlaglistsAsync();
        }

        private async Task DeleteSelectedFlaglistsAsync()
        {
            var flaglist = SelectedFlaglist;
            if (flaglist == null)
            {
                StatusMessage = "No flaglist selected.";
                return;
            }

            await DeleteFlaglistAsync(flaglist);
            await LoadPublishedFlaglistsAsync();

            EditableTitle = "";
            EditableJson = "";

            StatusMessage = "Selected flaglist deleted.";
        }

        private static bool IsMostlyFastFlags(string json)
        {
            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                if (dict == null || dict.Count == 0)
                    return false;

                string[] validPrefixes =
                {
                    "FFlag", "DFFlag", "FInt", "DFInt",
                    "FLog", "DFLog", "SFFlag", "FString", "DFString"
                };

                int matchingCount = dict.Keys.Count(key =>
                    validPrefixes.Any(prefix => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));

                return matchingCount >= dict.Count / 2;
            }
            catch
            {
                return false;
            }
        }

        private bool ContainsBannedWords(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            string lower = input.ToLowerInvariant();

            foreach (var bannedWord in BannedWords)
            {
                if (lower.Contains(bannedWord))
                    return true;
            }

            return false;
        }

        private static readonly string[] BannedWords = new[]
        {
            "nig", "nigga", "nigger", "Fuck", "fucking", "fucker", "fucked", "fucks", "MotherFucker", "motherfucking", "Bitch", "bitching", "bitcher", "bitches"
            // i swear to god if i get called racist cuz of auto mod i will crash out
        };

        private static bool IsWithinLineLimit(string json, int maxLines = 1250)
        {
            var lineCount = json.Split('\n').Length;
            return lineCount <= maxLines;
        }
    }
}