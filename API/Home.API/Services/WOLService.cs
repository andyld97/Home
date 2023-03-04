using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query;
using System.Net.NetworkInformation;

namespace Home.API.Services
{
    /// <summary>
    /// Wake On Lan Service for waking up clients<br/>
    /// Before using this service ensure that your client supports WOL!<br/>
    /// Reference: https://stackoverflow.com/a/58043033/6237448
    /// </summary>
    public interface IWOLService
    {
        /// <summary>
        /// Wakes up a PC over WOL
        /// </summary>
        /// <param name="macAddress">MacAddress in any standard HEX format (- or : as separator)</param>
        /// <returns></returns>
        Task SendWOLRequestAsync(string macAddress);

        /// <summary>
        /// Wakes up a PC over WOL
        /// </summary>
        /// <param name="macAddress">MacAddress in any standard HEX format (- or : as separator)</param>
        /// <param name="port">The port to be send to (7 or 9)</param>
        /// <returns></returns>
        Task SendWOLRequestAsync(string macAddress, int port);
    }

    /// <summary>
    /// <inheritdoc cref="IWOLService"/>
    /// </summary>
    public class WOLService : IWOLService
    {
        private readonly ILogger<WOLService> _logger;            

        public WOLService(ILogger<WOLService> logger)
        {
            _logger= logger;
        }

        #region Interface Methods

        public async Task SendWOLRequestAsync(string macAddress)
        {
            await SendWOLRequestAsync(macAddress, Program.GlobalConfig.WakeOnLanPort);
        }

        public async Task SendWOLRequestAsync(string macAddress, int port)
        {
            // The wol call differs on Windows/Linux, because the Windows version is based on NetworkInterface.GetAllNetworkInterfaces()
            // which might a be a little more reliable, but this API is also thought for using Linux, so it has to be compatible with both OSes!
            _logger.LogInformation($"Sending magick packet (WOL) to {macAddress}:{port}");

            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                await SendWolRequestWindowsAsync(macAddress, port);
            else
                await SendWolRequestNeutral(macAddress, port);
        }

        #endregion

        #region Wake On Lan Impl

        /// <summary>
        /// Wakes up a PC over WOL (platform neutral method, works on both Windows and Linux) [Broadcast]
        /// </summary>
        /// <param name="macAddress">MacAddress in any standard HEX format (- or : as separator)</param>
        private async Task SendWolRequestNeutral(string macAddress, int port)
        {
            var package = BuildMagicPacket(macAddress);
            var client = new UdpClient();
            client.EnableBroadcast = true;
            client.Send(package, package.Length, new IPEndPoint(IPAddress.Broadcast, port));
        }

        /// <summary>
        /// Wakes up a PC over WOL (using every possible eth-interface found) [Multicast]
        /// </summary>
        /// <param name="macAddress">MacAddress in any standard HEX format (- or : as separator)</param>
        /// <param name="port">The port to be send to (7 or 9)</param>
        private static async Task SendWolRequestWindowsAsync(string macAddress, int port)
        {
            byte[] magicPacket = BuildMagicPacket(macAddress);
            foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces().Where((n) =>
                n.NetworkInterfaceType != NetworkInterfaceType.Loopback && n.OperationalStatus == OperationalStatus.Up))
            {
                IPInterfaceProperties iPInterfaceProperties = networkInterface.GetIPProperties();
                foreach (MulticastIPAddressInformation multicastIPAddressInformation in iPInterfaceProperties.MulticastAddresses)
                {
                    IPAddress multicastIpAddress = multicastIPAddressInformation.Address;
                    if (multicastIpAddress.ToString().StartsWith("ff02::1%", StringComparison.OrdinalIgnoreCase)) // Ipv6: All hosts on LAN (with zone index)
                    {
                        UnicastIPAddressInformation unicastIPAddressInformation = iPInterfaceProperties.UnicastAddresses.Where((u) =>
                            u.Address.AddressFamily == AddressFamily.InterNetworkV6 && !u.Address.IsIPv6LinkLocal).FirstOrDefault();
                        if (unicastIPAddressInformation != null)
                        {
                            await SendWakeOnLanAsync(unicastIPAddressInformation.Address, multicastIpAddress, magicPacket, port);
                        }
                    }
                    else if (multicastIpAddress.ToString().Equals("224.0.0.1")) // Ipv4: All hosts on LAN
                    {
                        UnicastIPAddressInformation unicastIPAddressInformation = iPInterfaceProperties.UnicastAddresses.Where((u) =>
                            u.Address.AddressFamily == AddressFamily.InterNetwork && !iPInterfaceProperties.GetIPv4Properties().IsAutomaticPrivateAddressingActive).FirstOrDefault();
                        if (unicastIPAddressInformation != null)
                        {
                            await SendWakeOnLanAsync(unicastIPAddressInformation.Address, multicastIpAddress, magicPacket, port);
                        }
                    }
                }
            }
        }
        #endregion

        #region Wake On Lan Helper Methods

        /// <summary>
        /// Builds the "magic"-package
        /// </summary>
        /// <param name="macAddress">MacAddress in any standard HEX format (- or : as separator)</param>
        /// <returns>The "magic"-package as a byte-array</returns>
        private static byte[] BuildMagicPacket(string macAddress)
        {
            macAddress = Regex.Replace(macAddress, "[: -]", "");
            byte[] macBytes = Convert.FromHexString(macAddress);

            // First 6 times 0xFF:
            IEnumerable<byte> header = Enumerable.Repeat((byte)0xFF, 6);
            // Then 16 times MacAddress:
            IEnumerable<byte> data = Enumerable.Repeat(macBytes, 16).SelectMany(m => m);
            return header.Concat(data).ToArray();
        }

        /// <summary>
        /// Sends a WOL request (UDP; Multicast)
        /// </summary>
        /// <param name="localIpAddress">The local ip address</param>
        /// <param name="multicastIpAddress">Multicast address to send the package to</param>
        /// <param name="magicPacket">The built magic package</param>
        /// <param name="port">The port to be send to (7 or 9)</param>
        /// <returns></returns>
        private static async Task SendWakeOnLanAsync(IPAddress localIpAddress, IPAddress multicastIpAddress, byte[] magicPacket, int port)
        {
            using UdpClient client = new(new IPEndPoint(localIpAddress, 0));
            await client.SendAsync(magicPacket, magicPacket.Length, new IPEndPoint(multicastIpAddress, port));
        }

        #endregion
    }
}