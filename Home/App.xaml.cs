using ControlzEx.Theming;
using Helper;
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
        public delegate void onShutdownOrRestart(Device device, bool shutdown, bool wol);
        public static event onShutdownOrRestart OnShutdownOrRestart;

        protected override void OnStartup(StartupEventArgs e)
        {
#if DEBUG
            // Change lang to en to debug
            System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("en-US");
            System.Threading.Thread.CurrentThread.CurrentCulture =
            System.Threading.Thread.CurrentThread.CurrentUICulture = ci;
#endif

            // This is necessary if the languge will be changed before, than the menu won't get updated:
            var deviceMenu = FindResource("DeviceMenu") as ContextMenu;
            var first = deviceMenu.Items[0] as System.Windows.Controls.MenuItem;
            first.Header = Home.Properties.Resources.strWakeUp;

            var second = deviceMenu.Items[1] as System.Windows.Controls.MenuItem;
            second.Header = Home.Properties.Resources.strShutdown;

            var third = deviceMenu.Items[2] as System.Windows.Controls.MenuItem;
            third.Header = Home.Properties.Resources.strReboot;

            ThemeHelper.ApplyTheme();
            base.OnStartup(e);
        }

        private void MenuItemShutdown_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement).DataContext is Device d)
                OnShutdownOrRestart?.Invoke(d, true, false);            
        }
 
        private void MenuItemReboot_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement).DataContext is Device d)
                OnShutdownOrRestart?.Invoke(d, false, false);
        }

        private void MenuItemBoot_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement).DataContext is Device d)
                OnShutdownOrRestart?.Invoke(d, false, true);
        }
    }
}