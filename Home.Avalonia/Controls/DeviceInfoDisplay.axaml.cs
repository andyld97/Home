using Avalonia.Controls;
using Avalonia.Utilities;
using Home.Model;
using System.Collections;
using LiveChartsCore;
using Avalonia.Media;
using System.Linq;
using System;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView;
using System.Collections.Generic;
using Avalonia;

namespace Home.Avalonia.Controls
{
    public partial class DeviceInfoDisplay : UserControl
    {
        private Device currentDevice;
        public DeviceInfoDisplay()
        {
            InitializeComponent();
        }

        public DeviceInfoDisplay(Device device)
        {
            this.currentDevice = device;
            InitializeComponent();
            Refresh(device);           
        }
        public void Refresh(Device device)
        {
            this.currentDevice = device;
            DataContext = device;
            CmbGraphics.Items = device.Environment.GraphicCards;
            if (device.Environment.GraphicCards.Count > 0)
                CmbGraphics.SelectedIndex = 0;

            plot.Height = 200; 
            InitalizeDeviceActivityPlot();
            RenderPlot(currentDevice); 
        }
        private void InitalizeDeviceActivityPlot()
        {
            // This legend is always switching colors, so I am going to use my own legend!
            plot.LegendPosition = LiveChartsCore.Measure.LegendPosition.Hidden; // top
            plot.LegendOrientation = LiveChartsCore.Measure.LegendOrientation.Horizontal;
            //plot.LegendBackground = FindResource("Fluent.Ribbon.Brushes.White") as SolidColorBrush;
            //plot.LegendTextBrush = FindResource("Fluent.Ribbon.Brushes.Black") as SolidColorBrush;*/

            var xaxis = plot.XAxes.FirstOrDefault();
            var yaxis = plot.YAxes.FirstOrDefault();
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

        public void RenderPlot(Device currentDevice)
        {
            if (currentDevice == null)
                return;

            this.currentDevice = currentDevice;

            List<Point> cpuPoints = new List<Point>();
            List<Point> ramPoints = new List<Point>();
            List<Point> diskPoints = new List<Point>();
            List<Point> batteryPoints = new List<Point>();

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

            //  Fill = new SolidColorPaint(SkiaSharp.SKColors.LightBlue.WithAlpha(128)),

            void mapping(Point s, ChartPoint e)
            {
                e.SecondaryValue = s.X;
                e.PrimaryValue = s.Y;
            }

            var cpuSeries = new LineSeries<Point>()
            {
                Values = cpuPoints,
                Mapping = mapping,
                Stroke = new SolidColorPaint(SkiaSharp.SKColors.AliceBlue, 3),
                Fill = null,
                GeometrySize = 0,
                GeometryStroke = new SolidColorPaint(SkiaSharp.SKColors.AliceBlue, 3),
                Name = "CPU Usage (%)",
                TooltipLabelFormatter = (s) => $"CPU: {s.PrimaryValue} %",
            };

            var ramSeries = new LineSeries<Point>()
            {
                Values = ramPoints,
                Mapping = mapping,
                Stroke = new SolidColorPaint(SkiaSharp.SKColors.Violet, 3),
                Fill = null,
                GeometrySize = 0,
                GeometryStroke = new SolidColorPaint(SkiaSharp.SKColors.Violet, 3),
                Name = "RAM Usage (%)",
                TooltipLabelFormatter = (s) => $"RAM: {s.PrimaryValue} %",
            };

            var diskSeries = new LineSeries<Point>()
            {
                Values = diskPoints,
                Mapping = mapping,
                Stroke = new SolidColorPaint(SkiaSharp.SKColors.Orange, 3),
                Fill = null,
                GeometrySize = 0,
                GeometryStroke = new SolidColorPaint(SkiaSharp.SKColors.Orange, 3),
                Name = "DISK Usage (%)",
                TooltipLabelFormatter = (s) => $"DISK: {s.PrimaryValue} %",
            };

            var batterySeries = new LineSeries<Point>()
            {
                Values = batteryPoints,
                Mapping = mapping,
                Stroke = new SolidColorPaint(SkiaSharp.SKColors.Green, 3),
                Fill = null,
                GeometrySize = 0,
                GeometryStroke = new SolidColorPaint(SkiaSharp.SKColors.Green, 3),
                Name = "Battery Remaining (%)",
                TooltipLabelFormatter = (s) => $"Battery Remaining: {s.PrimaryValue} %"
            };

            List<ISeries> series = new List<ISeries>(); // { cpuSeries, ramSeries, diskSeries };

         //   if (ChkCPULegend.IsChecked.Value)
                series.Add(cpuSeries);

          //  if (ChkRAMLegend.IsChecked.Value)
                series.Add(ramSeries);

         //   if (ChkDiskLegend.IsChecked.Value)
                series.Add(diskSeries);

         //   if (ChkBatteryLegend.IsChecked.Value && currentDevice.BatteryInfo != null)
            if (currentDevice.BatteryInfo != null)
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
    }
}