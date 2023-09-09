using Home.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Speech.Recognition.SrgsGrammar;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Home.Controls.Dialogs
{
    /// <summary>
    /// Interaktionslogik für ManageDeviceSchedule.xaml
    /// </summary>
    public partial class ManageDeviceSchedule : Window
    {
        private bool isInitalized = false;
        private ObservableCollection<DeviceSchedulingRule> rules = new ObservableCollection<DeviceSchedulingRule>();
        private DeviceSchedulingRule currentRule = null;

        public ManageDeviceSchedule(IEnumerable<DeviceSchedulingRule> fetchedRules)
        {
            InitializeComponent();

            CmbDevices.ItemsSource = MainWindow.W_INSTANCE.GetDevices();

            if (fetchedRules != null)
                rules = new ObservableCollection<DeviceSchedulingRule>(fetchedRules);

            ListRules.ItemsSource = rules;
            isInitalized = true;
            
            if (rules.Count > 0)
                ListRules.SelectedIndex = 0;
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

            var rule = rules[ListRules.SelectedIndex];
            currentRule = rule;

            TabBoot.DataContext = rule.BootRule;
            TabShutdown.DataContext = rule.ShutdownRule;

            PanelBootAPICall.DataContext = rule.BootRule.RuleAPICallInfo;
            PanelShutdownAPICall.DataContext = rule.ShutdownRule.RuleAPICallInfo;
            PanelShutdownExecuteCommand.DataContext = rule.ShutdownRule.RuleCommandInfo;

            GridRule.DataContext = rule;

            var device = MainWindow.W_INSTANCE.GetDevices().Where(d => d.ID == rule.AssociatedDeviceId).FirstOrDefault();
            if (device != null)
                CmbDevices.SelectedItem = device;
            else
                CmbDevices.SelectedItem = null;

            // Always select the first "general"-tab
            MainTabControl.SelectedIndex = 0;
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
    #endregion
}