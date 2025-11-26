using Bloxstrap.Models.APIs.Config;

namespace Bloxstrap
{
    // Placeholder implementation to allow builds without the full account manager feature.
    public class AccountManager
    {
        public static AccountManager? PreloadedInstance { get; set; }

        public AltAccount? ActiveAccount { get; set; }
    }
}
