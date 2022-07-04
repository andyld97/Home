using Home.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace Model.PCI
{
    public class PCIDeviceItem : PCIDevice, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public new List<PCIDeviceItem> Children { get; set; } = new List<PCIDeviceItem>();

        private bool isExpanded;
        private bool isSelected;

        public bool IsExpanded
        {
            get => isExpanded;
            set
            {
                if (value != isExpanded)
                {
                    isExpanded = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (value != isSelected)
                {
                    isSelected = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static List<PCIDeviceItem> BuildViewModel(IEnumerable<PCIDevice> devices, bool isFirstCall, List<string> selectedHashes = null, List<string> expandedHashes = null)
        {
            List<PCIDeviceItem> result = new List<PCIDeviceItem>();
            PCIDeviceItem other = new PCIDeviceItem() { ID = "Other", Class = "PCI" };

            foreach (var item in devices)
            {
                var newItem = new PCIDeviceItem();
                newItem.ID = item.ID;
                newItem.Capabilites = item.Capabilites;
                newItem.Properties = item.Properties;
                newItem.Class = item.Class;
                newItem.Children = BuildViewModel(item.Children, isFirstCall: false, selectedHashes: selectedHashes, expandedHashes: expandedHashes);
                var hash = newItem.BuildHash();

                if (selectedHashes != null && selectedHashes.Any(y => y == hash))
                    newItem.IsSelected = true;

                if (expandedHashes != null && expandedHashes.Any(y => y == hash))
                    newItem.IsExpanded = true;

                if (isFirstCall)
                {
                    if (item.Children.Count > 0)
                        result.Add(newItem);
                    else
                        other.Children.Add(newItem);
                }
                else
                    result.Add(newItem);
            }

            if (isFirstCall)
            {
                var hash = other.BuildHash();

                if (selectedHashes.Any(x => x == hash))
                    other.IsSelected = true;
                if (expandedHashes.Any(x => x == hash))
                    other.IsExpanded = true;

                result.Add(other);
            }

            return result;
        }
    }
}