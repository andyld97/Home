using Home.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Home.Controls
{
    /// <summary>
    /// Interaktionslogik für DeviceInfoDisplay.xaml
    /// </summary>
    public partial class DeviceInfoDisplay : UserControl
    {
        public DeviceInfoDisplay()
        {
            InitializeComponent();
        }

        public void UpdateDevice(Device currentDevice)
        {
            DataContext = currentDevice;
            CmbGraphics.SelectedIndex = 0;
        }
    }
}
