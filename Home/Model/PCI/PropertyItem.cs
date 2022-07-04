using Home.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Model.PCI
{
    public class PropertyItem : Property, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public new List<PropertyItem> Values { get; set; } = new List<PropertyItem>();

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

        public static List<PropertyItem> BuildViewModel(IEnumerable<Property> properties, List<string> previouslySelectedItems, List<string> previouslyExpandedItems)
        {
            var result  = new List<PropertyItem>();

            foreach (var property in properties)
            {
                PropertyItem newItem = new PropertyItem();

                newItem.Name = property.Name;
                newItem.Value = property.Value;
                newItem.Values = BuildViewModel(property.Values, previouslySelectedItems, previouslyExpandedItems);

                var hash = property.BuildHash();
                if (previouslySelectedItems.Any(x => x == hash))
                    newItem.IsSelected = true;
                if (previouslyExpandedItems.Any(x => x == hash))
                    newItem.IsExpanded = true;

                result.Add(newItem);
            }

            return result;
        }
    }
}
