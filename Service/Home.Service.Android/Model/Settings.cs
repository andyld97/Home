namespace Home.Service.Android.Model
{
    public class Settings
    {
        public string Host { get; set; } = "http://192.168.178.38:83";

        public bool IsDeviceRegistered { get; set; } = false;

        public string WlanSSID { get; set; } = string.Empty;
    }
}