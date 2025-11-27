using Bloxstrap.Integrations;
using Bloxstrap.UI.ViewModels.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.Windows;

namespace Bloxstrap.UI.ViewModels.ContextMenu
{
    internal partial class GameInformationViewModel : ObservableObject
    {
        private readonly Watcher _watcher;
        private ActivityWatcher? _activityWatcher => _watcher.ActivityWatcher;

        [ObservableProperty]
        private string _gameName = "Loading...";

        [ObservableProperty]
        private string _gameDescription = "";

        [ObservableProperty]
        private BitmapImage? _gameThumbnail;

        [ObservableProperty]
        private string _creatorName = "Loading...";

        [ObservableProperty]
        private bool _isCreatorVerified;

        [ObservableProperty]
        private long _playingCount;

        [ObservableProperty]
        private long _visitsCount;

        [ObservableProperty]
        private long _favoritesCount;

        [ObservableProperty]
        private string _createdDate = "";

        [ObservableProperty]
        private string _updatedDate = "";

        [ObservableProperty]
        private string _genre = "";

        [ObservableProperty]
        private bool _isDataLoading = true;

        [ObservableProperty]
        private bool _hasError;

        [ObservableProperty]
        private string _errorMessage = "";

        [ObservableProperty]
        private ObservableCollection<PlaceInfo> _subplaces = new();

        [ObservableProperty]
        private bool _hasSubplaces;

        public GameInformationViewModel(Watcher watcher)
        {
            _watcher = watcher;

            if (_activityWatcher?.Data != null && _activityWatcher.InGame)
            {
                _ = LoadGameInformationAsync();
            }
            else
            {
                HasError = true;
                ErrorMessage = "Not currently in a game";
                IsDataLoading = false;
            }
        }

        private async Task LoadGameInformationAsync()
        {
            const string LOG_IDENT = "GameInformationViewModel::LoadGameInformation";

            try
            {
                IsDataLoading = true;
                HasError = false;

                if (_activityWatcher?.Data == null)
                {
                    throw new InvalidOperationException("No game activity data available");
                }

                long universeId = _activityWatcher.Data.UniverseId;
                long placeId = _activityWatcher.Data.PlaceId;

                if (universeId == 0 && placeId != 0)
                {
                    universeId = await GetUniverseIdFromPlaceId(placeId);
                }

                if (universeId == 0)
                {
                    throw new InvalidOperationException("Could not determine game universe ID");
                }

                await LoadUniverseDetailsAsync(universeId);

                await LoadSubplacesAsync(universeId);

            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Error loading game information: {ex.Message}");
                HasError = true;
                ErrorMessage = $"Failed to load game information: {ex.Message}";
            }
            finally
            {
                IsDataLoading = false;
            }
        }

        private async Task<long> GetUniverseIdFromPlaceId(long placeId)
        {
            const string LOG_IDENT = "GameInformationViewModel::GetUniverseIdFromPlaceId";

            try
            {
                var placeDetails = await FetchPlaceDetailsAsync(placeId);
                return placeDetails?.universeId ?? 0L;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Error getting universe ID: {ex.Message}");
                return 0L;
            }
        }

        private async Task LoadUniverseDetailsAsync(long universeId)
        {
            const string LOG_IDENT = "GameInformationViewModel::LoadUniverseDetails";

            try
            {
                await UniverseDetails.FetchBulk(universeId.ToString());
                var universeDetails = UniverseDetails.LoadFromCache(universeId);

                if (universeDetails?.Data != null)
                {
                    var data = universeDetails.Data;

                    GameName = data.Name ?? "Unknown Game";
                    GameDescription = data.Description ?? "No description available";

                    if (data.Creator != null)
                    {
                        CreatorName = data.Creator.Name ?? "Unknown Creator";
                        IsCreatorVerified = data.Creator.HasVerifiedBadge;
                    }
                    else
                    {
                        CreatorName = "Unknown Creator";
                        IsCreatorVerified = false;
                    }

                    PlayingCount = data.Playing;
                    VisitsCount = data.Visits;
                    FavoritesCount = data.FavoritedCount;

                    Genre = GetGenreName(data.Genre);

                    CreatedDate = data.Created.ToString("MMMM dd, yyyy");
                    UpdatedDate = data.Updated.ToString("MMMM dd, yyyy");
                }
                else
                {
                    throw new InvalidOperationException("Could not load universe details");
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Error loading universe details: {ex.Message}");
                throw;
            }
        }

        private async Task LoadSubplacesAsync(long universeId)
        {
            const string LOG_IDENT = "GameInformationViewModel::LoadSubplaces";

            try
            {
                if (universeId == 0)
                {
                    Subplaces.Clear();
                    HasSubplaces = false;
                    return;
                }

                using var client = new HttpClient();
                string url = $"https://develop.roblox.com/v1/universes/{universeId}/places?isUniverseCreation=false&limit=50&sortOrder=Asc";

                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Failed to fetch subplaces: {response.StatusCode}");
                    return;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var subplacesResponse = JsonSerializer.Deserialize<SubplacesResponse>(responseContent);

                if (subplacesResponse?.Data == null || !subplacesResponse.Data.Any())
                {
                    Subplaces.Clear();
                    HasSubplaces = false;
                    return;
                }

                var subplacesList = new List<PlaceInfo>();

                foreach (var place in subplacesResponse.Data)
                {
                    string thumbnailUrl = await GetPlaceThumbnailUrlAsync(place.Id);
                    subplacesList.Add(new PlaceInfo(place.Id, place.UniverseId, place.Name, thumbnailUrl));
                }

                Subplaces = new ObservableCollection<PlaceInfo>(subplacesList);
                HasSubplaces = Subplaces.Any();
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Error loading subplaces: {ex.Message}");
                Subplaces.Clear();
                HasSubplaces = false;
            }
        }

        private async Task<BitmapImage?> LoadBitmapFromUrlAsync(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return null;

            try
            {
                using var client = new HttpClient();
                var imageData = await client.GetByteArrayAsync(imageUrl);

                return await Task.Run(() =>
                {
                    try
                    {
                        using var stream = new MemoryStream(imageData);
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        return bitmap;
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                });
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<string> GetPlaceThumbnailUrlAsync(long placeId)
        {
            try
            {
                var thumbnailResponse = await Http.GetJson<ApiArrayResponse<ThumbnailResponse>>(
                    $"https://thumbnails.roblox.com/v1/places/gameicons?placeIds={placeId}&returnPolicy=PlaceHolder&size=128x128&format=Png&isCircular=false");

                var firstThumbnail = thumbnailResponse?.Data?.FirstOrDefault();
                return firstThumbnail?.ImageUrl ?? "";
            }
            catch (Exception)
            {
                return "";
            }
        }

        private async Task<PlaceDetails?> FetchPlaceDetailsAsync(long placeId)
        {
            try
            {
                using var client = new HttpClient();
                string url = $"https://games.roblox.com/v1/games/multiget-place-details?placeIds={placeId}";

                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var placeDetailsArray = JsonSerializer.Deserialize<List<PlaceDetails>>(responseContent);

                return placeDetailsArray?.FirstOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string GetGenreName(string? genre)
        {
            if (string.IsNullOrEmpty(genre))
                return "All";

            return genre switch
            {
                "All" => "All",
                "Building" => "Building",
                "Horror" => "Horror",
                "TownAndCity" => "Town and City",
                "Military" => "Military",
                "Comedy" => "Comedy",
                "Medieval" => "Medieval",
                "Adventure" => "Adventure",
                "SciFi" => "Sci-Fi",
                "Naval" => "Naval",
                "FPS" => "FPS",
                "RPG" => "RPG",
                "Sports" => "Sports",
                "Fighting" => "Fighting",
                "Western" => "Western",
                _ => genre
            };
        }

        [RelayCommand]
        private void CopyGameLink()
        {
            if (_activityWatcher?.Data?.PlaceId != null)
            {
                string gameUrl = $"https://www.roblox.com/games/{_activityWatcher.Data.PlaceId}";
                Clipboard.SetDataObject(gameUrl);
            }
        }

        [RelayCommand]
        private async Task RefreshInformation()
        {
            await LoadGameInformationAsync();
        }
    }
}