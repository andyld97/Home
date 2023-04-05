using Home.Data.Com;
using Newtonsoft.Json;
using System;
using System.Windows;

namespace Notification
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        private void Application_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {

        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Debug:
            // MessageBox.Show(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new Message("Dies ist ein Test", "TEst-Text", Message.MessageImage.Error)))));
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length >= 2)
            {
                try
                {
                    var message = JsonConvert.DeserializeObject<Message>(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(args[1])));
                    if (message != null)
                    {
                        MessageBoxImage image = MessageBoxImage.Information;
                        switch (message.Type)
                        {
                            case Message.MessageImage.Error: image = MessageBoxImage.Error; break;
                            case Message.MessageImage.Information: image = MessageBoxImage.Information; break;
                            case Message.MessageImage.Warning: image = MessageBoxImage.Warning; break;
                        }

                        MessageBox.Show(message.Content, message.Title, MessageBoxButton.OK, image, MessageBoxResult.OK, MessageBoxOptions.ServiceNotification);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            }
            Environment.Exit(0);
        }
    }
}