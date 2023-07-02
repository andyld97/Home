using Home.Data.Helper;
using Home.Model;
using System.Windows.Controls;

namespace Home.Controls.Warnings
{
    /// <summary>
    /// Interaktionslogik für StorageWarning.xaml
    /// </summary>
    public partial class StorageWarningItem : UserControl
    {
        public StorageWarningItem(StorageWarning sw)
        {
            InitializeComponent();
            TextName.Text = sw.DiskName ?? Home.Properties.Resources.strUnkown;
            TextSpace.Text = string.Format(Properties.Resources.strStorageWarning_Message, ByteUnit.FindUnit(sw.Value).ToString());
            TextDatum.Text = sw.WarningOccurred.ToString(Properties.Resources.strDateTimeFormat);
        }
    }
}