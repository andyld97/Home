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
            if (Settings.Instance.UseDarkMode)
            {
                App.Current.Resources["Item.SelectedColor"] = new SolidColorBrush(Colors.White);
            }
            else
            {
                App.Current.Resources["Item.SelectedColor"] = new SolidColorBrush(Colors.Gray);
            }

            ThemeManager.Current.ChangeTheme(Application.Current, GetCurrentTheme());
        }
    }
}