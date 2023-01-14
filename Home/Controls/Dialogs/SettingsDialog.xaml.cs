using ControlzEx.Theming;
using Helper;
using Home;
using Home.Data.Helper;
using Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Controls.Dialogs
{
    /// <summary>
    /// Interaction logic for SettingsDialog.xaml
    /// </summary>
    public partial class SettingsDialog : Window
    {
        private bool editMode;
        private bool forceExit = false;
        private bool isConnectionTested = false;
        public event PropertyChangedEventHandler PropertyChanged;

        public static Theme[] Themes
        {
            get
            {
                string value = Settings.Instance.UseDarkMode ? "Dark" : "Light";
                return ThemeManager.Current.Themes.Where(p => !p.DisplayName.Contains("Colorful") && p.DisplayName.Contains(value)).ToArray();
            }
        }

        public SettingsDialog(bool isConnectionTested)
        {
            editMode = true;
            InitializeComponent();

            LoadSettings();

            ComboBoxThemeChooser.DataContext = this;
            this.isConnectionTested = isConnectionTested;
        }

        private void LoadSettings()
        {
            editMode = true;

            CheckBoxActivateGlowingBrush.IsChecked = Settings.Instance.ActivateGlowingBrush;
            TextHost.Text = Settings.Instance.Host;
            SelectTheme();

            editMode = false;
        }

        private void SelectTheme()
        {
            // Get current selected theme
            string value = Settings.Instance.UseDarkMode ? "Dark" : "Light";
            CheckBoxDisplayMode.SelectedIndex = Settings.Instance.UseDarkMode ? 1 : 0;
            ComboBoxThemeChooser.SelectedItem = Themes.Where(p => p.DisplayName.Contains(Settings.Instance.Theme.Replace(".Colorful", string.Empty)) && p.DisplayName.Contains(value)).FirstOrDefault();
            CheckBoxThemeIsColorful.IsChecked = Settings.Instance.Theme.Contains(".Colorful");
        }

        private void CheckBoxDisplayMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (editMode)
                return;

            Settings.Instance.UseDarkMode = CheckBoxDisplayMode.SelectedIndex == 1;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Themes"));

            string value = Settings.Instance.UseDarkMode ? "Dark" : "Light";
            ComboBoxThemeChooser.ItemsSource = Themes;
            ComboBoxThemeChooser.SelectedItem = Themes.Where(p => p.DisplayName.Contains(Settings.Instance.Theme.Replace(".Colorful", string.Empty)) && p.DisplayName.Contains(value)).FirstOrDefault();

            Settings.Instance.Save();
            ThemeHelper.ApplyTheme();
        }

        private void UpdateThemeSettings()
        {
            if (ComboBoxThemeChooser.SelectedIndex == -1 || editMode)
                return;

            string theme = Themes[ComboBoxThemeChooser.SelectedIndex].Name.Replace("Light.", string.Empty).Replace("Dark.", string.Empty);

            if (CheckBoxThemeIsColorful.IsChecked.HasValue && CheckBoxThemeIsColorful.IsChecked.Value)
                theme += ".Colorful";

            Settings.Instance.Theme = theme;
            Settings.Instance.Save();

            // Apply theming
            ThemeHelper.ApplyTheme();
            MainWindow.W_INSTANCE.UpdateGlowingBrush();
        }

        private void ComboBoxThemeChooser_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (editMode || ComboBoxThemeChooser.SelectedIndex == -1)
                return;

            UpdateThemeSettings();
        }

        private void CheckBoxThemeIsColorful_Checked(object sender, RoutedEventArgs e)
        {
            if (editMode)
                return;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Themes)));
            UpdateThemeSettings();
        }

        private void CheckBoxThemeIsColorful_Unchecked(object sender, RoutedEventArgs e)
        {
            if (editMode)
                return;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Themes)));
            UpdateThemeSettings();
        }

        private void CheckBoxActivateGlowingBrush_Checked(object sender, RoutedEventArgs e)
        {
            if (editMode)
                return;

            Settings.Instance.ActivateGlowingBrush = CheckBoxActivateGlowingBrush.IsChecked.Value;
            Settings.Instance.Save();
            UpdateThemeSettings();
        }

        private void TextHost_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (editMode)
                return;

            isConnectionTested = false;
            Settings.Instance.Host = TextHost.Text;
            Settings.Instance.Save();
        }

        private async void ButtonTestConnection_Click(object sender, RoutedEventArgs e)
        {
            var api = new Home.Communication.API(Settings.Instance.Host);
            var result = await api.TestConnectionAsync();
          
            if (result.Item1)
            {
                isConnectionTested = true;
                MessageBox.Show(Home.Properties.Resources.strSettings_ConnectionEstablishedSuccuessfully, Home.Properties.Resources.strSuccess, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
                MessageBox.Show($"{Home.Properties.Resources.strSettings_ConnectionNotEstablished}: {result.Item2}", Home.Properties.Resources.strError, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (forceExit)
                return;

            if (!isConnectionTested)
            {
                e.Cancel = true;
                MessageBox.Show(Home.Properties.Resources.strSettings_OnlyContinueUsingAValidConnection, Home.Properties.Resources.strError, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else
                DialogResult = true;
        }

        private void ButtonForceExit_Click(object sender, RoutedEventArgs e)
        {
            forceExit = true;
            DialogResult = false;
            Application.Current.Shutdown();
        }
    }
}