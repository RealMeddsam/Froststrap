using DiscordRPC;

namespace Bloxstrap.Integrations
{
    public class FroststrapRichPresence : IDisposable
    {
        private readonly DiscordRpcClient _rpcClient;
        private readonly Timestamps _startTimestamps;

        public FroststrapRichPresence()
        {
            _rpcClient = new DiscordRpcClient("1399535282713399418");

            _rpcClient.OnReady += (_, e) =>
                App.Logger.WriteLine("FroststrapRichPresence", $"Connected as {e.User.Username}");

            _rpcClient.OnError += (_, e) =>
                App.Logger.WriteLine("FroststrapRichPresence", $"RPC error: {e.Message}");

            _rpcClient.Initialize();

            _startTimestamps = new Timestamps
            {
                Start = DateTime.UtcNow
            };

            SetPresence();
        }

        private void SetPresence()
        {
            UpdatePresence("Idle");
        }

        public void UpdatePresence(string context)
        {
            var presence = new DiscordRPC.RichPresence
            {
                Details = "Customize Roblox to your liking!",
                State = context,
                Timestamps = _startTimestamps,
                Assets = new Assets
                {
                    LargeImageKey = "Froststrap",
                    LargeImageText = "Froststrap"
                },
                Buttons = new[]
                {
                    new Button { Label = "GitHub", Url = "https://github.com/RealMeddsam/Froststrap" },
                    new Button { Label = "Discord", Url = "https://discord.gg/KdR9vpRcUN" }
                }
            };

            _rpcClient.SetPresence(presence);
        }

        public void ResetPresence()
        {
            UpdatePresence("Idle");
        }

        public void Dispose()
        {
            if (_rpcClient == null)
                return;

            App.Logger.WriteLine("FroststrapRichPresence::Dispose", "Clearing presence and disposing RPC client");

            try
            {
                // Only attempt to clear if client is not already disposed
                _rpcClient.ClearPresence();
            }
            catch (ObjectDisposedException)
            {
                // already disposed, ignore
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("FroststrapRichPresence::Dispose", $"Error clearing presence: {ex}");
            }

            try
            {
                _rpcClient.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // ignore double-dispose
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("FroststrapRichPresence::Dispose", $"Error disposing RPC client: {ex}");
            }

            GC.SuppressFinalize(this);
        }
    }
}