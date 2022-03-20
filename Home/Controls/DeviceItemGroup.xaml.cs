﻿using Home.Helper;
using Home.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Data;

namespace Home.Controls
{
    /// <summary>
    /// Interaction logic for DeviceItemGroup.xaml
    /// </summary>
    public partial class DeviceItemGroup : UserControl, INotifyPropertyChanged
    {
        private string groupName;
        private List<Device> devices = new List<Device>();

        public string GroupName
        {
            get => groupName;
            set
            {
                if (groupName != value)
                {
                    groupName = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<Device> Devices
        {
            get => devices;
            set
            {
                if (devices != value)
                {
                    devices = value;
                    OnPropertyChanged();
                }
            }
        }

        public DeviceItemGroup()
        {
            InitializeComponent();
            DataContext = this;            
        }

        #region INotfiyPropertyChanged Impl
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

    #region Converter

    public class DeviceImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Device d)
                return ImageHelper.LoadImage($"pack://application:,,,/Home;Component/resources/icons/devices/{d.DetermineDeviceImage()}");

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    #endregion
}