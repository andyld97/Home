using ControlzEx.Theming;
using Home;
using Home.Model;
using Home.Properties;
using LiveChartsCore.Themes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Settings = Home.Model.Settings;

namespace Helper
{
    public static class ThemeHelper
    {
        private static string GetCurrentTheme()
        {
            return $"{(Settings.Instance.UseDarkMode ? ThemeManager.BaseColorDarkConst : ThemeManager.BaseColorLightConst)}.{Settings.Instance.Theme}"; //.Colorful";
        }

        public static void ApplyTheming()
        {
            System.Windows.Media.Color 
                tabControlBackgroundColor, tabItemBackground, tabItemSelectedBackground;

            static Color ToColor(string hex) => (Color)ColorConverter.ConvertFromString(hex);
            static Brush ToBrush(string hex) => new SolidColorBrush(ToColor(hex)); 

            if (Settings.Instance.UseDarkMode)
            {
                App.Current.Resources["Item.SelectedColor"] = new SolidColorBrush(Colors.White);
                tabControlBackgroundColor = ToColor("#171616");

                tabItemBackground = ToColor("#AF282828");
                tabItemSelectedBackground = ToColor("#282828");

                // Scrollbar
                App.Current.Resources["ScrollBarButtonBackgroundBrush"] = ToBrush("#FF2B2B2B");
                App.Current.Resources["ScrollbarThumb"] = ToBrush("#FF383838");
                App.Current.Resources["ScrollBarButtonHighlightBackgroundBrush"] = ToBrush("#FF3C3C3C");
                App.Current.Resources["ScrollBarButtonArrowForegroundBrush"] = ToBrush("#FFD0D0D0");
                App.Current.Resources["ScrollBarTrackBrush"] = ToBrush("#FF1C1C1C");
            }
            else
            {
                App.Current.Resources["Item.SelectedColor"] = new SolidColorBrush(Colors.Gray);
                tabControlBackgroundColor = ToColor("#DADADA");

                tabItemBackground = ToColor("#D7D7D7");
                tabItemSelectedBackground = ToColor("#F9F9F9");
            }

            App.Current.Resources["TabControl.Background"] = new SolidColorBrush(tabControlBackgroundColor);
            App.Current.Resources["TabItemBackground"] = new SolidColorBrush(tabItemBackground);
            App.Current.Resources["TabItemSelectedBackground"] = new SolidColorBrush(tabItemSelectedBackground);

            var theme = ThemeManager.Current.GetTheme(GetCurrentTheme());
            if (theme != null)
            {
                if (Settings.Instance.UseDarkMode)
                {
                    var fixedBlack = (Color)ColorConverter.ConvertFromString("#FF252525");
                    theme.Resources["Fluent.Ribbon.Colors.White"] = fixedBlack;
                    theme.Resources["Fluent.Ribbon.Brushes.White"] = new SolidColorBrush(fixedBlack);
                }

                ThemeManager.Current.ChangeTheme(Application.Current, theme);
                return;
            }

            ThemeManager.Current.ChangeTheme(Application.Current, GetCurrentTheme());
        }
    }
}