using Home.Model;
using System.Windows.Controls;

namespace Home.Controls.Warnings
{
    /// <summary>
    /// Interaktionslogik für BatteryWarningItem.xaml
    /// </summary>
    public partial class BatteryWarningItem : UserControl
    {
        public BatteryWarningItem(BatteryWarning warning)
        {
            InitializeComponent();
            TextPercentage.Text = string.Format(Home.Properties.Resources.strBatteryWarning_Message, warning.Value.ToString());
        }
    }
}