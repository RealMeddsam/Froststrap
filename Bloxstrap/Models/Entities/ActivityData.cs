using Bloxstrap.AppData;
using Bloxstrap.Models.APIs;
using Bloxstrap.Models.APIs.RoValra;
using CommunityToolkit.Mvvm.Input;
using System.DirectoryServices.ActiveDirectory;
using System.Runtime.InteropServices;
using System.Web;
using System.Windows;
using System.Windows.Input;

namespace Bloxstrap.Models.Entities
{
    public class ActivityData
    {

        private long _universeId = 0;

        /// <summary>
        /// If the current activity stems from an in-universe teleport, then this will be
        /// set to the activity that corresponds to the initial game join
        /// </summary>
        public ActivityData? RootActivity;

        public long UniverseId
        {
            get => _universeId;
            set
            {
                _universeId = value;
                UniverseDetails.LoadFromCache(value);
            }
        }

        public long PlaceId { get; set; } = 0;

        public string JobId { get; set; } = string.Empty;

        /// <summary>
        /// This will be empty unless the server joined is a private server
        /// </summary>
        public string AccessCode { get; set; } = string.Empty;
        
        public long UserId { get; set; } = 0;

        public string MachineAddress { get; set; } = string.Empty;

        public bool MachineAddressValid => !string.IsNullOrEmpty(MachineAddress) && !MachineAddress.StartsWith("10.");

        public bool IsTeleport { get; set; } = false;

        public ServerType ServerType { get; set; } = ServerType.Public;

        public DateTime TimeJoined { get; set; }

        public DateTime? TimeLeft { get; set; }

        // everything below here is optional strictly for bloxstraprpc, discord rich presence, or game history

        /// <summary>
        /// This is intended only for other people to use, i.e. context menu invite link, rich presence joining
        /// </summary>
        public string RPCLaunchData { get; set; } = string.Empty;

        public UniverseDetails? UniverseDetails { get; set; }

        public ICommand CopyJobIdCommand => new RelayCommand(CopyJobId);

        private void CopyJobId()
        {
            if (!string.IsNullOrEmpty(JobId))
            {
                Clipboard.SetText(JobId);
            }
        }

        public ICommand CopyJoinLinkCommand => new RelayCommand(CopyJoinLink);

        public ICommand RejoinServerCommand => new RelayCommand(RejoinServer);

        private SemaphoreSlim serverQuerySemaphore = new(1, 1);
        private SemaphoreSlim serverTimeSemaphore = new(1, 1);

        public string GetInviteDeeplink(bool launchData = true)
        {
            string deeplink = $"https://www.roblox.com/games/start?placeId={PlaceId}";

            if (ServerType == ServerType.Private) // thats not going to work
                deeplink += "&accessCode=" + AccessCode;
            else
                deeplink += "&gameInstanceId=" + JobId;

            if (launchData && !string.IsNullOrEmpty(RPCLaunchData))
                deeplink += "&launchData=" + HttpUtility.UrlEncode(RPCLaunchData);

            return deeplink;
        }

        public async Task<DateTime?> QueryServerTime()
        {
            const string LOG_IDENT = "ActivityData::QueryServerTime";

            if (string.IsNullOrEmpty(JobId))
                throw new InvalidOperationException("JobId is null");

            if (PlaceId == 0)
                throw new InvalidOperationException("PlaceId is null");

            await serverTimeSemaphore.WaitAsync();

            if (GlobalCache.ServerTime.TryGetValue(JobId, out DateTime? time))
            {
                serverTimeSemaphore.Release();
                return time;
            }

            DateTime? firstSeen = DateTime.UtcNow;
            try
            {
                var serverTimeRaw = await Http.GetJson<RoValraTimeResponse>($"https://apis.rovalra.com/v1/server_details?place_id={PlaceId}&server_ids={JobId}");

                var serverBody = new RoValraProcessServerBody
                {
                    PlaceId = PlaceId,
                    ServerIds = new() { JobId }
                };

                string json = JsonSerializer.Serialize(serverBody);
                HttpContent postContent = new StringContent(json, Encoding.UTF8, "application/json");

                // we dont need to await it since its not as important
                // we want to return uptime quickly
                _ = App.HttpClient.PostAsync("https://apis.rovalra.com/process_servers", postContent);


                RoValraServer? server = null;

                if (serverTimeRaw?.Servers != null && serverTimeRaw.Servers.Count > 0)
                    server = serverTimeRaw.Servers[0];

                // if the server hasnt been registered we will simply return UtcNow
                // since firstSeen is UtcNow by default we dont have to check anything else
                if (server?.FirstSeen != null)
                    firstSeen = server.FirstSeen;

                GlobalCache.ServerTime[JobId] = firstSeen;
                serverTimeSemaphore.Release();
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to get server time for {PlaceId}/{JobId}");
                App.Logger.WriteException(LOG_IDENT, ex);

                GlobalCache.ServerTime[JobId] = firstSeen;
                serverTimeSemaphore.Release();

                Frontend.ShowConnectivityDialog(
                    string.Format(Strings.Dialog_Connectivity_UnableToConnect, "rovalra.com"),
                    Strings.ActivityWatcher_LocationQueryFailed,
                    MessageBoxImage.Warning,
                    ex
                );
            }

            return firstSeen;
        }

        public async Task<string?> QueryServerLocation()
        {
            const string LOG_IDENT = "ActivityData::QueryServerLocation";

            if (!MachineAddressValid)
                throw new InvalidOperationException($"Machine address is invalid ({MachineAddress})");

            await serverQuerySemaphore.WaitAsync();

            if (GlobalCache.ServerLocation.TryGetValue(MachineAddress, out string? location))
            {
                serverQuerySemaphore.Release();
                return location;
            }

            try
            {
                var ipInfo = await Http.GetJson<IPInfoResponse>($"https://ipinfo.io/{MachineAddress}/json");

                if (string.IsNullOrEmpty(ipInfo.City))
                    throw new InvalidHTTPResponseException("Reported city was blank");

                if (ipInfo.City == ipInfo.Region)
                    location = $"{ipInfo.Region}, {ipInfo.Country}";
                else
                    location = $"{ipInfo.City}, {ipInfo.Region}, {ipInfo.Country}";

                GlobalCache.ServerLocation[MachineAddress] = location;
                serverQuerySemaphore.Release();
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to get server location for {MachineAddress}");
                App.Logger.WriteException(LOG_IDENT, ex);

                GlobalCache.ServerLocation[MachineAddress] = location;
                serverQuerySemaphore.Release();

                Frontend.ShowConnectivityDialog(
                    string.Format(Strings.Dialog_Connectivity_UnableToConnect, "ipinfo.io"),
                    Strings.ActivityWatcher_LocationQueryFailed,
                    MessageBoxImage.Warning,
                    ex
                );
            }

            return location;
        }

        public override string ToString() => $"{PlaceId}/{JobId}";

        private void RejoinServer()
        {
            try
            {
                App.Logger.WriteLine("ActivityData::RejoinServer", $"Rejoining server: {PlaceId}/{JobId}");

                string robloxUri = $"roblox://experiences/start?placeId={PlaceId}&gameInstanceId={JobId}";

                if (ServerType == ServerType.Private && !string.IsNullOrEmpty(AccessCode))
                {
                    robloxUri += $"&accessCode={AccessCode}";
                }

                if (!string.IsNullOrEmpty(RPCLaunchData))
                {
                    robloxUri += $"&launchData={HttpUtility.UrlEncode(RPCLaunchData)}";
                }

                App.Logger.WriteLine("ActivityData::RejoinServer", $"Launching Roblox URI: {robloxUri}");

                Process.Start(new ProcessStartInfo
                {
                    FileName = robloxUri,
                    UseShellExecute = true
                });

                App.Logger.WriteLine("ActivityData::RejoinServer", "Successfully launched new Roblox instance");

                CloseExistingRobloxInstances();
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("ActivityData::RejoinServer", ex);
                MessageBox.Show($"Failed to rejoin server: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseExistingRobloxInstances()
        {
            try
            {
                var processes = Process.GetProcessesByName("RobloxPlayerBeta");
                int closedCount = 0;

                foreach (var process in processes)
                {
                    try
                    {
                        if ((DateTime.Now - process.StartTime).TotalSeconds < 3)
                        {
                            App.Logger.WriteLine("ActivityData::CloseExistingRobloxInstances", $"Skipping new process (PID: {process.Id})");
                            continue;
                        }

                        App.Logger.WriteLine("ActivityData::CloseExistingRobloxInstances", $"Instantly closing old Roblox process (PID: {process.Id})");
                        process.Kill(); // Kill instantly, don't wait for exit
                        closedCount++;
                    }
                    catch (Exception ex)
                    {
                        App.Logger.WriteLine("ActivityData::CloseExistingRobloxInstances", $"Failed to close process {process.Id}: {ex.Message}");
                    }
                }

                App.Logger.WriteLine("ActivityData::CloseExistingRobloxInstances", $"Instantly closed {closedCount} old Roblox instances");
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("ActivityData::CloseExistingRobloxInstances", $"Error closing processes: {ex.Message}");
            }
        }

        private void CopyJoinLink()
        {
            try
            {
                string joinLink = GetInviteDeeplink();
                Clipboard.SetText(joinLink);

                App.Logger.WriteLine("ActivityData::CopyJoinLink", $"Copied join link: {joinLink}");
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("ActivityData::CopyJoinLink", ex);
                MessageBox.Show($"Failed to copy join link: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public string ServerTypeDisplay => ServerType switch
        {
            ServerType.Public => "Public Server",
            ServerType.Private => "Private Server",
            ServerType.Reserved => "Reserved Server",
            _ => "Unknown Server"
        };

        public string ServerIdDisplay
        {
            get
            {
                if (string.IsNullOrEmpty(JobId))
                    return "No Server ID";

                if (JobId.Length > 12)
                    return $"Server: {JobId[..12]}...";
                else
                    return $"Server: {JobId}";
            }
        }

    }
}
