using ControlzEx.Theming;
using Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

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
            ThemeManager.Current.ChangeTheme(Application.Current, GetCurrentTheme());
        }
    }
}