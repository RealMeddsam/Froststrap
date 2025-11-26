namespace Bloxstrap.Models.APIs.Config
{
    // Minimal placeholder to satisfy account manager data deserialization.
    public class AltAccount
    {
        public long Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? AuthToken { get; set; }
    }
}
