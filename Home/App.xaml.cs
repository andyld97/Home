using ControlzEx.Theming;
using Home.Model;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Home
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        public delegate void onShutdownOrRestart(Device device, bool shutdown);
        public static event onShutdownOrRestart OnShutdownOrRestart;

        protected override void OnStartup(StartupEventArgs e)
        {

            // Change lang to en to debug
            System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("en-US");
            System.Threading.Thread.CurrentThread.CurrentCulture =
            System.Threading.Thread.CurrentThread.CurrentUICulture = ci;

            ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
            ThemeManager.Current.SyncTheme();

            // Ensure tooltips doesn't dissapear
            // Maybe this can be removed (because the bug should already be fixed in .NET Core 6.0.5)
            ToolTipService.ShowDurationProperty.OverrideMetadata(typeof(DependencyObject), new FrameworkPropertyMetadata(Int32.MaxValue));
            base.OnStartup(e);
        }

        private void MenuItemShutdown_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement).DataContext is Device d)
                OnShutdownOrRestart?.Invoke(d, true);            
        }
 
        private void MenuItemReboot_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement).DataContext is Device d)
                OnShutdownOrRestart?.Invoke(d, false);
        }
    }
}