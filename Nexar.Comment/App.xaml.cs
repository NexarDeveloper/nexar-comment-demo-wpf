using Nexar.Client;
using Nexar.Client.Login;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace Nexar.Comment
{
    public partial class App : Application
    {
        public static IReadOnlyList<IMyWorkspace> Workspaces { get; private set; }

        public static string Username => Login.Username;

        public static LoginInfo Login { get; private set; }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var args = e.Args;
            if (args.Length > 1)
            {
                MessageBox.Show("Usage: [endpoint]", Config.MyTitle, MessageBoxButton.OK, MessageBoxImage.Asterisk);
                Environment.Exit(1);
            }

            // custom api endpoint
            if (args.Length > 0 && !args[0].StartsWith("-"))
                Config.ApiEndpoint = args[0];
        }

        /// <summary>
        /// Run this as a task after the window is shown.
        /// </summary>
        /// <remarks>
        /// Why not before as it used to be. If a user does not login and maybe closes the login page,
        /// then the application is running waiting for the login. The user may not know about it.
        /// So, with the window shown the user is aware of it and may close.
        /// </remarks>
        public static async Task LoginAsync()
        {
            try
            {
                var clientId = Environment.GetEnvironmentVariable("NEXAR_CLIENT_ID") ?? throw new InvalidOperationException("Please set environment 'NEXAR_CLIENT_ID'");
                var clientSecret = Environment.GetEnvironmentVariable("NEXAR_CLIENT_SECRET") ?? throw new InvalidOperationException("Please set environment 'NEXAR_CLIENT_SECRET'");
                Login = await LoginHelper.LoginAsync(
                    clientId,
                    clientSecret,
                    new string[] { "user.access", "design.domain" },
                    Config.Authority);

                NexarClientFactory.AccessToken = Login.AccessToken;
            }
            catch (Exception ex)
            {
                ShowException(ex);
                Environment.Exit(1);
            }
        }

        public static async Task LoadWorkspacesAsync()
        {
            try
            {
                var client = NexarClientFactory.GetClient(Config.ApiEndpoint);
                var res = await client.Workspaces.ExecuteAsync();
                ClientHelper.EnsureNoErrors(res);
                Workspaces = res.Data.DesWorkspaces;
            }
            catch (Exception ex)
            {
                ShowException(ex);
                Environment.Exit(1);
            }
        }

        public static void ShowException(Exception ex)
        {
            var message = $"{ex.Message}\n\n{ex}";
            MessageBox.Show(message, Config.MyTitle, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.ServiceNotification);
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            ShowException(e.Exception);
            e.Handled = true;
        }
    }
}
