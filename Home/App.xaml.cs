using ControlzEx.Theming;
using System.Windows;

namespace Home
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
            ThemeManager.Current.SyncTheme();

            base.OnStartup(e);
        }
    }
}
