﻿using ControlzEx.Theming;
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
            ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
            ThemeManager.Current.SyncTheme();

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