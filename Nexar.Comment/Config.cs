using System;

namespace Nexar.Comment
{
    /// <summary>
    /// App configuration.
    /// </summary>
    static class Config
    {
        public const string MyTitle = "Nexar.Comment";

        public static string Authority { get; }
        public static string ApiEndpoint { get; }
        public static string AccessToken { get; set; }

        static Config()
        {
            Authority = Environment.GetEnvironmentVariable("NEXAR_AUTHORITY") ?? "https://identity.nexar.com";
            ApiEndpoint = Environment.GetEnvironmentVariable("NEXAR_API_URL") ?? "https://api.nexar.com/graphql";
        }

        /// <summary>
        /// Gets true if subscription is supported.
        /// Not yet supported by public services.
        /// </summary>
        public static bool IsSubscription => !ApiEndpoint.Contains("//api.");
    }
}
