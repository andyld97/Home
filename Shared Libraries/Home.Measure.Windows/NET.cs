using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Home.Measure.Windows
{
    /// <summary>
    /// Capsels all methods available by .NET
    /// </summary>
    public static class NET
    {
        public static string DetermineIPAddress()
        {
            string returnAddress = string.Empty;

            try
            {
                // Get a list of all network interfaces (usually one per network card, dialup, and VPN connection)
                NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

                foreach (NetworkInterface network in networkInterfaces)
                {
                    // Read the IP configuration for each network
                    IPInterfaceProperties properties = network.GetIPProperties();

                    string description = network.Description.ToLower();

                    if ((network.NetworkInterfaceType == NetworkInterfaceType.Ethernet || network.NetworkInterfaceType == NetworkInterfaceType.Wireless80211) && network.OperationalStatus == OperationalStatus.Up && !description.Contains("virtual") && !description.Contains("pseudo"))
                    {
                        // Each network interface may have multiple IP addresses
                        foreach (IPAddressInformation address in properties.UnicastAddresses)
                        {
                            // We're only interested in IPv4 addresses for now
                            if (address.Address.AddressFamily != AddressFamily.InterNetwork)
                                continue;

                            // Ignore loopback addresses (e.g., 127.0.0.1)
                            if (IPAddress.IsLoopback(address.Address))
                                continue;

                            returnAddress = address.Address.ToString();
                        }
                    }
                }
            }
            catch
            {

            }

            return returnAddress;
        }

        public static byte[] CreateScreenshot(string fileName)
        {
            try
            {
                // ToDo: *** Respect dpi settings?
                double screenLeft = System.Windows.Forms.SystemInformation.VirtualScreen.Left; // SystemParameters.VirtualScreenLeft; [.net 4.8]
                double screenTop = System.Windows.Forms.SystemInformation.VirtualScreen.Top; // SystemParameters.VirtualScreenTop; [.net 4.8]
                double screenWidth = System.Windows.Forms.SystemInformation.VirtualScreen.Width; // SystemParameters.VirtualScreenWidth; [.net 4.8]
                double screenHeight = System.Windows.Forms.SystemInformation.VirtualScreen.Height; // SystemParameters.VirtualScreenHeight; [.net 4.8]

                using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap((int)screenWidth, (int)screenHeight))
                {
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen((int)screenLeft, (int)screenTop, 0, 0, bmp.Size);
                        bmp.Save(fileName);
                    }
                }

                byte[] result = System.IO.File.ReadAllBytes(fileName);
                try
                {
                    System.IO.File.Delete(fileName);
                }
                catch
                {

                }


                return result;
                // ToDO: log
            }
            catch (Exception ex)
            {
                // ToDo: Log
             
            }

            return null;
        }
    }
}
