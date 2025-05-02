using Home.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Home.Controls.Dialogs
{
    /// <summary>
    /// Interaktionslogik für ManageDeviceSchedule.xaml
    /// </summary>
    public partial class ManageDeviceSchedule : Window
    {
        private bool isInitalized = false;
        private bool ignoreCheckedChanged = false;
        private ObservableCollection<DeviceSchedulingRule> rules = new ObservableCollection<DeviceSchedulingRule>();
        private DeviceSchedulingRule currentRule = null;

        private List<CheckBox> chkBootDays = [];
        private List<CheckBox> chkShutdownDays = [];

        public ManageDeviceSchedule(IEnumerable<DeviceSchedulingRule> fetchedRules)
        {
            InitializeComponent();

            RefreshDevices();
            chkBootDays = [ChkBootMonday, ChkBootTuesday, ChkBootWednesday, ChkBootThursday, ChkBootFriday, ChkBootSaturday, ChkBootSunday];
            chkShutdownDays = [ChkShutdownMonday, ChkShutdownTuesday, ChkShutdownWednesday, ChkShutdownThursday, ChkShutdownFriday, ChkShutdownSaturday, ChkShutdownSunday];

            if (fetchedRules != null)
                rules = new ObservableCollection<DeviceSchedulingRule>(fetchedRules);

            ListRules.ItemsSource = rules;
            isInitalized = true;
            
            if (rules.Count > 0)
                ListRules.SelectedIndex = 0;
        }

        private void RefreshDevices()
        {
            var devices = MainWindow.W_INSTANCE.GetDevices();

            if (!string.IsNullOrEmpty(SearchDeviceTextBox.Text))
                devices = devices.Where(d => d.Name.Contains(SearchDeviceTextBox.Text, StringComparison.CurrentCultureIgnoreCase)).ToList();

            CmbDevices.ItemsSource = devices;
        }

        private void ListRules_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isInitalized)
                return;

            if (rules.Count > 0)
                GridRule.IsEnabled = true;
            else
            {
                GridRule.IsEnabled = true;
                return;
            }

            if (ListRules.SelectedIndex == -1)
                ListRules.SelectedIndex = 0;

            ignoreCheckedChanged = true;
            var rule = rules[ListRules.SelectedIndex];
            currentRule = rule;

            TabBoot.DataContext = rule.BootRule;
            TabShutdown.DataContext = rule.ShutdownRule;

            PanelBootAPICall.DataContext = rule.BootRule.RuleAPICallInfo;
            PanelShutdownAPICall.DataContext = rule.ShutdownRule.RuleAPICallInfo;
            PanelShutdownExecuteCommand.DataContext = rule.ShutdownRule.RuleCommandInfo;

            GridRule.DataContext = rule;

            // BootRule
            if (currentRule.BootRule.ExecutionDaysPlan.Daily)
                RadioBootDaily.IsChecked = true;
            else
                RadioBootDays.IsChecked = true;

            PanelBootCheckBoxDays.DataContext = currentRule.BootRule.ExecutionDaysPlan;

            for (int i = 0; i <  currentRule.BootRule.ExecutionDaysPlan.Days.Length; i++)
                chkBootDays[i].IsChecked = currentRule.BootRule.ExecutionDaysPlan.Days[i];

            // ShutdownRule
            if (currentRule.ShutdownRule.ExecutionDaysPlan.Daily)
                RadioShutdownDaily.IsChecked = true;
            else
                RadioShutdownDays.IsChecked = true;

            PanelShutdownCheckBoxDays.DataContext = currentRule.ShutdownRule.ExecutionDaysPlan;

            for (int i = 0; i < currentRule.ShutdownRule.ExecutionDaysPlan.Days.Length; i++)
                chkShutdownDays[i].IsChecked = currentRule.ShutdownRule.ExecutionDaysPlan.Days[i];

            var device = MainWindow.W_INSTANCE.GetDevices().FirstOrDefault(d => d.ID == rule.AssociatedDeviceId);
            if (device != null)
                CmbDevices.SelectedItem = device;
            else
                CmbDevices.SelectedItem = null;

            // Always select the first "general"-tab
            MainTabControl.SelectedIndex = 0;
            ignoreCheckedChanged = false;
        }

        private void ButtonAddRule_Click(object sender, RoutedEventArgs e)
        {
            rules.Add(new DeviceSchedulingRule() { Name = Home.Properties.Resources.strManageDeviceSchedulingRulesDialog_EmptyRule });
        }

        private void CmbDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isInitalized)
                return;

            currentRule.AssociatedDeviceId = ((Device)CmbDevices.SelectedItem)?.ID;
        }

        private void ButtonRemoveRule_Click(object sender, RoutedEventArgs e)
        {
            if (ListRules.SelectedIndex == -1)
                return;

            if (MessageBox.Show(string.Format(Home.Properties.Resources.strManageDeviceSchedulingRulesDialog_SureToDeleteRule, currentRule.Name), Properties.Resources.strReally, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                rules.Remove(currentRule);
                if (rules.Count == 0)
                    GridRule.IsEnabled = false;
            }
        }

        private void ButtonDuplicateRule_Click(object sender, RoutedEventArgs e)
        {
            var rule = new DeviceSchedulingRule()
            {
                Name = currentRule.Name + " - Copy",
                AssociatedDeviceId = currentRule.AssociatedDeviceId,
                Description = currentRule.Description,
                IsActive = currentRule.IsActive,
                CustomMacAddress = currentRule.CustomMacAddress,
                BootRule = (BootRule)currentRule.BootRule.Clone(),
                ShutdownRule = (ShutdownRule)currentRule.ShutdownRule.Clone()
            };

            rules.Add(rule);

            ListRules.SelectedIndex = rules.Count - 1;
            currentRule = rule;
        }

        private async void ButtonApply_Click(object sender, RoutedEventArgs e)
        {
            var result = await MainWindow.API.UpdateSchedulingRules(rules);

            if (!result.Success)
            {
                MessageBox.Show(string.Format(Home.Properties.Resources.strManageDeviceSchedulingRulesDialog_FailedToSaveRules, result.ErrorMessage), Properties.Resources.strError, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else 
                MessageBox.Show(Home.Properties.Resources.strSuccessfullyAppliedChanges, Properties.Resources.strSuccess, MessageBoxButton.OK, MessageBoxImage.Information);  

            closing = true;
            DialogResult = true;
        }

        #region Closing Dialog

        private bool closing = false;

        private bool AskSecurityQuestion()
        {
            return MessageBox.Show(Home.Properties.Resources.strManageDeviceSchedulingRulesDialog_SecurityQuestion, Properties.Resources.strReally, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        private void ButtoCancel_Click(object sender, RoutedEventArgs e)
        {
            if (!AskSecurityQuestion()) return;

            closing = true;
            DialogResult = false;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (closing)
                return;
            e.Cancel = true;
            if (AskSecurityQuestion())
                e.Cancel = false;            
        }
        #endregion

        #region WeekDays

        #region Boot Rule Events

        private void ChkBootWeekday_Checked(object sender, RoutedEventArgs e)
        {
            if (currentRule == null || ignoreCheckedChanged) return;

            int index = chkBootDays.IndexOf((CheckBox)sender);
            currentRule.BootRule.ExecutionDaysPlan.Days[index] = true;
        }

        private void ChkBootWeekday_Unchecked(object sender, RoutedEventArgs e)
        {
            if (currentRule == null || ignoreCheckedChanged) return;

            int index = chkBootDays.IndexOf((CheckBox)sender);
            currentRule.BootRule.ExecutionDaysPlan.Days[index] = false;
        }

        private void RadioBootDaily_Checked(object sender, RoutedEventArgs e)
        {
            if (currentRule == null || ignoreCheckedChanged) return;

            currentRule.BootRule.ExecutionDaysPlan.Daily = true;    
        }

        private void RadioBootDays_Checked(object sender, RoutedEventArgs e)
        {
            if (currentRule == null || ignoreCheckedChanged) return;

            currentRule.BootRule.ExecutionDaysPlan.Daily = false;
        }

        #endregion

        #region Shutdown Rule Events

        private void ChkShutdownWeek_Checked(object sender, RoutedEventArgs e)
        {
            if (currentRule == null || ignoreCheckedChanged) return;

            int index = chkShutdownDays.IndexOf((CheckBox)sender);
            currentRule.ShutdownRule.ExecutionDaysPlan.Days[index] = true;
        }

        private void ChkShutdownWeek_Unchecked(object sender, RoutedEventArgs e)
        {
            if (currentRule == null || ignoreCheckedChanged) return;

            int index = chkShutdownDays.IndexOf((CheckBox)sender);
            currentRule.ShutdownRule.ExecutionDaysPlan.Days[index] = false;
        }

        private void RadioShutdownDaily_Checked(object sender, RoutedEventArgs e)
        {
            if (currentRule == null || ignoreCheckedChanged) return;

            currentRule.ShutdownRule.ExecutionDaysPlan.Daily = true;
        }

        private void RadioShutdownDays_Checked(object sender, RoutedEventArgs e)
        {
            if (currentRule == null || ignoreCheckedChanged) return;

            currentRule.ShutdownRule.ExecutionDaysPlan.Daily = false;
        }

        #endregion

        #endregion

        private void PlaceholderTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshDevices();
        }
    }

    #region Converter

    public class TypeToReadOnlyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is BootRule.BootRuleType brType && brType == BootRule.BootRuleType.None)
                return !true;
            else if (value is ShutdownRule.ShutdownRuleType srType && srType == ShutdownRule.ShutdownRuleType.None)
                return !true;

            return !false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TypeToPlanVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is BootRule.BootRuleType brType && brType == BootRule.BootRuleType.None)
                return Visibility.Collapsed;
            else if (value is ShutdownRule.ShutdownRuleType srType && srType == ShutdownRule.ShutdownRuleType.None)
                return Visibility.Collapsed;

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EnumToIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return 0;

            return (int)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var values = Enum.GetValues(targetType);
            if (value is int i)
                return values.GetValue(i);

            return null;
        }
    }

    public class RuleToApiVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is BootRule.BootRuleType brType)
            {
                if (parameter?.ToString() == "visibility")
                {
                    if (brType == BootRule.BootRuleType.ExternalAPICall)
                        return Visibility.Visible;
                }
            }
            else if (value is ShutdownRule.ShutdownRuleType srType)
            {
                if (parameter?.ToString() == "visibility")
                {
                    if (srType == ShutdownRule.ShutdownRuleType.ExternalAPICall)
                        return Visibility.Visible;
                }
                else if (srType == ShutdownRule.ShutdownRuleType.ExecuteCommand)
                    return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DisabledRuleOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && !b)
                return 0.6;

            return 1.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DeviceImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Device d)
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = d.GetImage();
                bitmapImage.EndInit();

                return bitmapImage;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DeviceIDResolutionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string guid)
            {
                var devices = MainWindow.W_INSTANCE.GetDevices();
                var device = devices.FirstOrDefault(d => d.ID == guid);
                if (device == null)
                    return "<unkown>";

                return device.Name; 
            }

            return "<unkown>";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverterdBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion
}