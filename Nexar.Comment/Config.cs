using System;

namespace Nexar.Comment
{
    /// <summary>
    /// App configuration.
    /// </summary>
    static class Config
    {
        public const string MyTitle = "Nexar.Comment";

        public static NexarMode NexarMode { get; }
        public static string Authority { get; }
        public static string ApiEndpoint { get; set; }

        static Config()
        {
            // default mode
            var mode = Environment.GetEnvironmentVariable("NEXAR_MODE") ?? "Prod";
            NexarMode = (NexarMode)Enum.Parse(typeof(NexarMode), mode, true);

            // init mode related data
            switch (NexarMode)
            {
                case NexarMode.Prod:
                    Authority = "https://identity.nexar.com/";
                    ApiEndpoint = "https://api.nexar.com/graphql/";
                    break;
                default:
                    throw new Exception();
            }
        }

        /// <summary>
        /// Gets true if subscription is supported.
        /// Not yet supported by Gateway.
        /// </summary>
        public static bool IsSubscription =>
            ApiEndpoint.IndexOf("//api.") < 0;
    }

    public enum NexarMode
    {
        Prod,
    }
}
