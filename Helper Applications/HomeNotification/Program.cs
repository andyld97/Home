using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json;

namespace ToastNotification
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Debug:
            // MessageBox.Show(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new Home.Data.Com.Message("Hello World!", "Test-Message", Home.Data.Com.Message.MessageImage.Error)))));

            string[] args = Environment.GetCommandLineArgs();
            if (args.Length >= 2)
            {
                try
                {
                    var message = JsonConvert.DeserializeObject<Home.Data.Com.Message>(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(args[1])));
                    if (message != null)
                    {
                        LogLevel level = LogLevel.Information;
                        switch (message.Type)
                        {
                            case Home.Data.Com.Message.MessageImage.Information: level = LogLevel.Information; break;
                            case Home.Data.Com.Message.MessageImage.Warning: level = LogLevel.Warning; break;
                            case Home.Data.Com.Message.MessageImage.Error: level = LogLevel.Error; break;
                        }
                        
                        ShowMessage(message.Content, message.Title, level);
                    }
                }
                catch { }
            }
        }

        public static void ShowMessage(string message, string title, LogLevel level)
        {
            try
            {
                // System.InvalidOperationException: "Failed initializing notifications"
                // IOException: Ein dauerhafter Unterschlüssel kann nicht unter einem temporären übergeordneten Schlüssel erstellt werden.
                // https://github.com/CommunityToolkit/WindowsCommunityToolkit/issues/4858
                ShowToast(message, title, level);
            }
            catch
            {
                // Fallback                             
                MessageBoxIcon icon = MessageBoxIcon.Information;
                switch (level)
                {
                    case LogLevel.Error: icon = MessageBoxIcon.Error; break;
                    case LogLevel.Information: icon = MessageBoxIcon.Information; break;
                    case LogLevel.Warning: icon = MessageBoxIcon.Warning; break;
                }

                MessageBox.Show(message, title, MessageBoxButtons.OK, icon, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
            }
        }

        public static void ShowToast(string message, string title, LogLevel level)
        {
            // Inline Images are only supported from local path in unpackaged apps
            // see: https://github.com/CommunityToolkit/WindowsCommunityToolkit/issues/4734#issuecomment-1227821151

            string icon = string.Empty;
            switch (level)
            {
                case LogLevel.Warning: icon = "warning.png"; break;
                case LogLevel.Information: icon = "info.png"; break;
                case LogLevel.Error: icon = "error.png"; break;
            }

            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, icon);

            new ToastContentBuilder()
              .AddAppLogoOverride(new Uri(path))
              .AddText(title)
              .AddText(message)
              .Show();
        }

        public enum LogLevel
        {
            Information,
            Warning,
            Error,
        }
    }
}