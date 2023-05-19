using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;
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
using Home.Model;
using SkiaSharp;
using Home.Data.Helper;
using Model;
using System.Diagnostics;
using SkiaSharp.Views.WPF;

namespace Home.Controls
{
    /// <summary>
    /// Interaktionslogik für DeviceActivityPlot.xaml
    /// </summary>
    public partial class DeviceActivityPlot : UserControl
    {
        private Device currentDevice;
        private SolidColorPaint white = new SolidColorPaint(SKColors.White);
        private SolidColorPaint black = new SolidColorPaint(SKColors.Black);

        private Axis xaxis, yaxis;

        private bool? darkMode = null;

        public DeviceActivityPlot()
        {
            InitializeComponent();
            InitalizeDeviceActivityPlot();
        }         

        private void InitalizeDeviceActivityPlot()
        {
            // This legend is always switching colors, so I am going to use my own legend!
            xaxis =  plot.XAxes.FirstOrDefault() as Axis;
            yaxis = plot.YAxes.FirstOrDefault() as Axis;            

            // Make tooltip smaller (https://github.com/beto-rodriguez/LiveCharts2/releases/tag/v2.0.0-beta.700)
            plot.TooltipTextSize = 12;

            yaxis.Labeler = (y) => $"{y}%";
            xaxis.Labeler = (x) =>
            {
                if (currentDevice == null)
                    return string.Empty;

                if (currentDevice.LastSeen == DateTime.MinValue)
                    return String.Empty;

                // 60 is not true if there are not 60 values in the list
                // and remember that all values (cpu, ram, disk) MUST have the same amount, also if they get cleared (they get all cleared)
                var n = currentDevice.LastSeen.AddMinutes(-(currentDevice.Usage.CPU.Count - x));
                return n.ToString("HH:mm");
            };
        }

        private static List<SKColor> darkColors = new List<SKColor>() { SKColors.AliceBlue, SKColors.Violet, SKColors.Orange, SKColors.Green };
        private static List<SKColor> lightColors = new List<SKColor>() { SKColors.LightGray, SKColors.Violet, SKColors.Orange, SKColors.Green };

        public void RenderPlot(Device currentDevice)
        {
            if (currentDevice == null)
                return;

            this.currentDevice = currentDevice;

            if (darkMode == null || (darkMode != null && darkMode != Settings.Instance.UseDarkMode))
            {
                // Paint will only be changed if dark mode is changed.
                // If LabelsPaint will be set every time in this method, the labels are not visible anymore
                var paint = Settings.Instance.UseDarkMode ? white : black;

                xaxis.LabelsPaint =
                yaxis.LabelsPaint = paint;
                darkMode = Settings.Instance.UseDarkMode;

                if (darkMode == true)
                    PathCPU.Fill = new SolidColorBrush(darkColors[0].ToColor());
                else
                    PathCPU.Fill = new SolidColorBrush(lightColors[0].ToColor());
            } 

            List<Point> cpuPoints = new List<Point>();
            List<Point> ramPoints = new List<Point>();
            List<Point> diskPoints = new List<Point>();
            List<Point> batteryPoints = new List<Point>();

            List<SKColor> colors = new List<SKColor>();
            if (Settings.Instance.UseDarkMode)
                colors.AddRange(darkColors);
            else
                colors.AddRange(lightColors);

            int cpuCounter = 1;
            foreach (var cpu in currentDevice.Usage.CPU)
                cpuPoints.Add(new Point(cpuCounter++, NormalizeValue(cpu)));

            int ramCounter = 1;
            foreach (var ram in currentDevice.Usage.RAM)
                ramPoints.Add(new Point(ramCounter++, Math.Round((ram / currentDevice.Environment.TotalRAM) * 100, 2)));

            int diskCounter = 1;
            foreach (var disk in currentDevice.Usage.DISK)
                diskPoints.Add(new Point(diskCounter++, NormalizeValue(disk)));

            if (currentDevice.BatteryInfo != null)
            {
                int batteryCounter = 1;
                foreach (var bPercent in currentDevice.Usage.Battery)
                    batteryPoints.Add(new Point(batteryCounter++, bPercent));
            }

            void mapping(Point s, ChartPoint e)
            {
                e.SecondaryValue = s.X;
                e.PrimaryValue = s.Y;
            }

            // ToDo Localized
            var cpuSeries = new LineSeries<Point>()
            {
                Values = cpuPoints,
                Mapping = mapping,
                Stroke = new SolidColorPaint(colors[0], 3),
                Fill = null,
                GeometrySize = 0,
                Name = Home.Properties.Resources.strDeviceActivityPlot_CPU_Name,
                TooltipLabelFormatter = (s) => string.Format(Home.Properties.Resources.strDeviceActivityPlot_CPU_Tooltip, Math.Round(s.PrimaryValue, 0)),
            };

            var ramSeries = new LineSeries<Point>()
            {
                Values = ramPoints,
                Mapping = mapping,
                Stroke = new SolidColorPaint(colors[1], 3),
                Fill = null,
                GeometrySize = 0,
                Name = Home.Properties.Resources.strDeviceActivityPlot_RAM_Name,
                TooltipLabelFormatter = (s) => string.Format(Home.Properties.Resources.strDeviceActivityPlot_RAM_Tooltip, Math.Round(s.PrimaryValue, 0)),
            };

            var diskSeries = new LineSeries<Point>()
            {
                Values = diskPoints,
                Mapping = mapping,
                Stroke = new SolidColorPaint(colors[2], 3),
                Fill = null,
                GeometrySize = 0,
                Name = Home.Properties.Resources.strDeviceActivityPlot_DISK_Name,
                TooltipLabelFormatter = (s) => string.Format(Home.Properties.Resources.strDeviceActivityPlot_DISK_Tooltip, Math.Round(s.PrimaryValue, 0)),
            };

            var batterySeries = new LineSeries<Point>()
            {
                Values = batteryPoints,
                Mapping = mapping,
                Stroke = new SolidColorPaint(colors[3], 3),
                Fill = null,
                GeometrySize = 0,
                Name = Home.Properties.Resources.strDeviceActivityPlot_Battery_Name,
                TooltipLabelFormatter = (s) => string.Format(Home.Properties.Resources.strDeviceActivityPlot_Battery_Tooltip, Math.Round(s.PrimaryValue, 0))
            };

            List<ISeries> series = new List<ISeries>();

            if (ChkCPULegend.IsChecked.Value)
                series.Add(cpuSeries);

            if (ChkRAMLegend.IsChecked.Value)
                series.Add(ramSeries);

            if (ChkDiskLegend.IsChecked.Value)
                series.Add(diskSeries);

            if (ChkBatteryLegend.IsChecked.Value && currentDevice.BatteryInfo != null)
                series.Add(batterySeries);

            plot.Series = series;
        }

        private double NormalizeValue(double input)
        {
            // 0.19 => 19%
            if (Math.Round(input, 0) == 0)
                return input * 100;
            
            // 5000% => 50%
            if (input > 1000)
                return Math.Round(input / 1000, 2);

            // 500% => 50%
            if (input > 100)
                return Math.Round(input / 100, 2);

            return input;
        }

        #region Legend Checkboxes

        private void ChkCPULegend_Checked(object sender, RoutedEventArgs e)
        {
            RenderPlot(currentDevice);
        }

        private void ChkRAMLegend_Checked(object sender, RoutedEventArgs e)
        {
            RenderPlot(currentDevice);
        }

        private void ChkDiskLegend_Checked(object sender, RoutedEventArgs e)
        {
            RenderPlot(currentDevice);
        }

        private void ChkBatteryLegend_Checked(object sender, RoutedEventArgs e)
        {
            RenderPlot(currentDevice);
        }

        #endregion
    }
}