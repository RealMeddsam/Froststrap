using System.Windows.Input;
using Bloxstrap.Integrations;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Threading;

namespace Bloxstrap.UI.ViewModels.ContextMenu
{
    internal class ServerHistoryViewModel : NotifyPropertyChangedViewModel
    {
        private readonly ActivityWatcher _activityWatcher;
        private List<ActivityData> _allHistory = new();
        private readonly Dispatcher _dispatcher;

        public ObservableCollection<ActivityData> DisplayedHistory { get; private set; } = new();
        public List<ActivityData>? GameHistory { get; private set; }

        public GenericTriState LoadState { get; private set; } = GenericTriState.Unknown;
        public string Error { get; private set; } = String.Empty;
        public bool HasHistory => DisplayedHistory.Count > 0;

        public Dictionary<string, SortOption> SortOptions { get; } = new()
        {
            ["Newest First"] = SortOption.NewestFirst,
            ["Oldest First"] = SortOption.OldestFirst,
            ["Game Name"] = SortOption.GameName
        };

        public Dictionary<string, FilterOption> FilterOptions { get; } = new()
        {
            ["All Games"] = FilterOption.All,
            ["Public Servers"] = FilterOption.Public,
            ["Private Servers"] = FilterOption.Private,
            ["Reserved Servers"] = FilterOption.Reserved
        };

        private SortOption _selectedSortOption = SortOption.NewestFirst;
        public SortOption SelectedSortOption
        {
            get => _selectedSortOption;
            set
            {
                _selectedSortOption = value;
                ApplySortingAndFiltering();
                OnPropertyChanged(nameof(SelectedSortOption));
            }
        }

        private FilterOption _selectedFilterOption = FilterOption.All;
        public FilterOption SelectedFilterOption
        {
            get => _selectedFilterOption;
            set
            {
                _selectedFilterOption = value;
                ApplySortingAndFiltering();
                OnPropertyChanged(nameof(SelectedFilterOption));
            }
        }

        public string HistoryStats => $"{DisplayedHistory.Count} of {_allHistory.Count} games shown";

        public ICommand CloseWindowCommand => new RelayCommand(RequestClose);
        public ICommand RefreshCommand => new RelayCommand(RefreshData);
        public ICommand ClearHistoryCommand => new RelayCommand(ClearHistory);

        public EventHandler? RequestCloseEvent;

        public ServerHistoryViewModel(ActivityWatcher activityWatcher)
        {
            _activityWatcher = activityWatcher;
            _dispatcher = Dispatcher.CurrentDispatcher;

            LoadPersistentHistory();

            _activityWatcher.OnGameLeave += OnGameLeaveHandler;

            _ = LoadDataAsync();
        }

        private void LoadPersistentHistory()
        {
            try
            {
                if (App.Settings.Prop.ServerHistory != null && App.Settings.Prop.ServerHistory.Any())
                {
                    var storedHistory = App.Settings.Prop.ServerHistory
                        .Where(item => item != null)
                        .Take(10) // Only take last 10 from storage
                        .ToList();

                    foreach (var storedItem in storedHistory)
                    {
                        if (!_activityWatcher.History.Any(h =>
                            h.PlaceId == storedItem.PlaceId &&
                            h.JobId == storedItem.JobId &&
                            h.TimeJoined == storedItem.TimeJoined))
                        {
                            _activityWatcher.History.Insert(0, storedItem);
                        }
                    }

                    App.Logger.WriteLine("ServerHistoryViewModel::LoadPersistentHistory",
                        $"Loaded {storedHistory.Count} games from persistent storage");
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("ServerHistoryViewModel::LoadPersistentHistory", ex);
            }
        }

        private void SavePersistentHistory()
        {
            try
            {
                var historyToSave = _activityWatcher.History
                    .Take(10)
                    .ToList();

                App.Settings.Prop.ServerHistory = historyToSave;

                App.Settings.Save();

                App.Logger.WriteLine("ServerHistoryViewModel::SavePersistentHistory",
                    $"Saved {historyToSave.Count} games to persistent storage");
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("ServerHistoryViewModel::SavePersistentHistory", ex);
            }
        }

        private async void OnGameLeaveHandler(object? sender, EventArgs e)
        {
            await Task.Delay(100);
            await LoadDataAsync();

            SavePersistentHistory();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                await _dispatcher.InvokeAsync(() =>
                {
                    LoadState = GenericTriState.Unknown;
                    OnPropertyChanged(nameof(LoadState));
                });

                var historyEntries = _activityWatcher.History.ToList();

                var entriesNeedingDetails = historyEntries.Where(x => x.UniverseDetails is null).ToList();

                if (entriesNeedingDetails.Any())
                {
                    var universeIds = entriesNeedingDetails.Select(x => x.UniverseId).Distinct().Where(id => id > 0);

                    if (universeIds.Any())
                    {
                        string universeIdsString = string.Join(',', universeIds);

                        try
                        {
                            await UniverseDetails.FetchBulk(universeIdsString);
                        }
                        catch (Exception ex)
                        {
                            App.Logger.WriteException("ServerHistoryViewModel::LoadData", ex);
                            SetErrorState($"Failed to load game details: {ex.Message}");
                            return;
                        }

                        foreach (var entry in entriesNeedingDetails)
                        {
                            if (entry.UniverseId > 0)
                            {
                                entry.UniverseDetails = UniverseDetails.LoadFromCache(entry.UniverseId);
                            }
                        }
                    }
                }

                ProcessHistory(historyEntries);

                await _dispatcher.InvokeAsync(() =>
                {
                    ApplySortingAndFiltering();
                    LoadState = GenericTriState.Successful;
                    OnPropertyChanged(nameof(LoadState));
                    OnPropertyChanged(nameof(HasHistory));
                    OnPropertyChanged(nameof(HistoryStats));
                });
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("ServerHistoryViewModel::LoadData", ex);
                SetErrorState($"An error occurred: {ex.Message}");
            }
        }

        private void ProcessHistory(List<ActivityData> historyEntries)
        {
            _allHistory = new List<ActivityData>(historyEntries);
            var consolidatedEntries = new HashSet<ActivityData>();

            foreach (var entry in historyEntries)
            {
                if (entry.RootActivity is not null && !consolidatedEntries.Contains(entry))
                {
                    // Update root activity with the latest time and job id
                    if (entry.TimeLeft.HasValue && entry.RootActivity.TimeLeft.HasValue &&
                        entry.TimeLeft.Value > entry.RootActivity.TimeLeft.Value)
                        entry.RootActivity.TimeLeft = entry.TimeLeft;

                    if (entry.ServerType == ServerType.Public && !string.IsNullOrEmpty(entry.JobId))
                        entry.RootActivity.JobId = entry.JobId;

                    consolidatedEntries.Add(entry);
                }
            }

            _allHistory.RemoveAll(entry => consolidatedEntries.Contains(entry));
        }

        private void ApplySortingAndFiltering()
        {
            var filtered = _selectedFilterOption switch
            {
                FilterOption.Public => _allHistory.Where(x => x.ServerType == ServerType.Public),
                FilterOption.Private => _allHistory.Where(x => x.ServerType == ServerType.Private),
                FilterOption.Reserved => _allHistory.Where(x => x.ServerType == ServerType.Reserved),
                _ => _allHistory.AsEnumerable()
            };

            var sorted = _selectedSortOption switch
            {
                SortOption.OldestFirst => filtered.OrderBy(x => x.TimeJoined),
                SortOption.GameName => filtered.OrderBy(x => x.UniverseDetails?.Data.Name ?? "Unknown"),
                _ => filtered.OrderByDescending(x => x.TimeJoined) // NewestFirst
            };

            DisplayedHistory.Clear();
            foreach (var item in sorted)
            {
                DisplayedHistory.Add(item);
            }
        }

        private void RefreshData()
        {
            _ = LoadDataAsync();
        }

        private void ClearHistory()
        {
            _activityWatcher.History.Clear();
            _allHistory.Clear();
            DisplayedHistory.Clear();

            // Clear persistent storage too
            App.Settings.Prop.ServerHistory = new List<ActivityData>();
            App.Settings.Save();

            OnPropertyChanged(nameof(HasHistory));
            OnPropertyChanged(nameof(HistoryStats));
        }

        private void SetErrorState(string error)
        {
            _dispatcher.Invoke(() =>
            {
                Error = error;
                OnPropertyChanged(nameof(Error));
                LoadState = GenericTriState.Failed;
                OnPropertyChanged(nameof(LoadState));
            });
        }

        private void RequestClose()
        {
            SavePersistentHistory();

            _activityWatcher.OnGameLeave -= OnGameLeaveHandler;
            RequestCloseEvent?.Invoke(this, EventArgs.Empty);
        }
    }

    public enum SortOption
    {
        NewestFirst,
        OldestFirst,
        GameName
    }

    public enum FilterOption
    {
        All,
        Public,
        Private,
        Reserved
    }
}