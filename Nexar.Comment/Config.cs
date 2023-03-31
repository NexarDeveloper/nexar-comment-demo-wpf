using System;

namespace Nexar.Comment
{
    /// <summary>
    /// App configuration.
    /// </summary>
    static class Config
    {
        public const string MyTitle = "Nexar.Comment";

        public static string Authority { get; private set; }
        public static string ApiEndpoint { get; private set; }

        static Config()
        {
            Authority = Environment.GetEnvironmentVariable("NEXAR_AUTHORITY") ?? "https://identity.nexar.com";
            ApiEndpoint = Environment.GetEnvironmentVariable("NEXAR_API_URL") ?? "https://api.nexar.com/graphql";
        }

        /// <summary>
        /// Gets true if subscription is supported.
        /// Not yet supported by Gateway.
        /// </summary>
        public static bool IsSubscription =>
            ApiEndpoint.IndexOf("//api.") < 0;
    }
}
