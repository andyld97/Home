using ControlzEx.Theming;
using Home;
using Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Helper
{
    public static class ThemeHelper
    {
        private static string GetCurrentTheme()
        {
            return $"{(Settings.Instance.UseDarkMode ? ThemeManager.BaseColorDarkConst : ThemeManager.BaseColorLightConst)}.{Settings.Instance.Theme}"; //.Colorful";
        }

        public static void ApplyTheme()
        {
            System.Windows.Media.Color tabControlBackgroundColor, tabItemBackground, tabItemSelectedBackground;

            if (Settings.Instance.UseDarkMode)
            {
                App.Current.Resources["Item.SelectedColor"] = new SolidColorBrush(Colors.White);
                tabControlBackgroundColor = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#171616");

                tabItemBackground = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#AF282828");
                tabItemSelectedBackground = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#282828");
            }
            else
            {
                App.Current.Resources["Item.SelectedColor"] = new SolidColorBrush(Colors.Gray);
                tabControlBackgroundColor = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#DADADA");

                tabItemBackground = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#D7D7D7");
                tabItemSelectedBackground = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#F9F9F9");
            }

            App.Current.Resources["TabControl.Background"] = new SolidColorBrush(tabControlBackgroundColor);
            App.Current.Resources["TabItemBackground"] = new SolidColorBrush(tabItemBackground);
            App.Current.Resources["TabItemSelectedBackground"] = new SolidColorBrush(tabItemSelectedBackground);

            ThemeManager.Current.ChangeTheme(Application.Current, GetCurrentTheme());
        }
    }
}