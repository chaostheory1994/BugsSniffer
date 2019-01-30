using PacketDotNet;
using SharpPcap;
using SharpPcap.AirPcap;
using SharpPcap.LibPcap;
using SharpPcap.WinPcap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BugsSniffer.Api
{
    public static class ProgramCode
    {
        private static readonly int TIMEOUT = 1000;
        private static Boolean stopCapturing = false;

        private static HttpClient _client = new HttpClient(); 

        public static async Task Run()
        {
            // Print SharpPcap version
            String ver = SharpPcap.Version.VersionString;
            Console.WriteLine("Bugs! Sniffer using SharpPcap {0}", ver);

            // Setup HttpClient
            Console.WriteLine("Setting up Http Connection");
            _client.BaseAddress = new Uri("http://bp-aod.bugs.gscdn.com/");

            // Retrieve the device list
            var devices = CaptureDeviceList.Instance;

            // If no devices were found print an error
            if (devices.Count < 1)
            {
                Console.WriteLine("No devices were found on this machine");
                return;
            }

            Console.WriteLine();
            Console.WriteLine("The following devices are available on this machine:");
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine();

            Int32 i = 0;

            // Print out the devices
            foreach (var dev in devices)
            {
                /* Description */
                Console.WriteLine("{0}) {1} {2}", i, dev.Name, dev.Description);
                i++;
            }

            Console.WriteLine();
            Console.Write("-- Please choose a device to capture: ");
            i = Int32.Parse(Console.ReadLine());

            // Register a cancle handler that lets us break out of our capture loop
            Console.CancelKeyPress += HandleCancelKeyPress;

            var device = devices[i];

            // Open the device for capturing
            device.Open(DeviceMode.Promiscuous, TIMEOUT);

            Console.WriteLine();
            Console.WriteLine("-- Listening on {0}, hit 'ctrl-c' to stop...",
                device.Name);

            while (stopCapturing == false)
            {
                var rawCapture = device.GetNextPacket();

                // null packets can be returned in the case where
                // the GetNextRawPacket() timed out, we should just attempt
                // to retrieve another packet by looping the while() again
                if (rawCapture == null)
                {
                    // go back to the start of the while()
                    continue;
                }
                try
                {
                    // use PacketDotNet to parse this packet and print out
                    // its high level information
                    Packet p = Packet.ParsePacket(rawCapture.LinkLayerType, rawCapture.Data);

                    var tcpPacket = (TcpPacket)p.Extract(typeof(TcpPacket));
                    if (tcpPacket != null)
                    {
                        string packetData = tcpPacket.PayloadData != null ? Encoding.ASCII.GetString(tcpPacket.PayloadData) : "Empty";

                        if (packetData.Contains("GET") && packetData.Contains("bp-aod.bugs.gscdn.com"))
                        {
                            IpPacket ipPacket = (IpPacket)tcpPacket.ParentPacket;
                            Console.WriteLine($"{rawCapture.Timeval:hh:mm:ss,fff} Len={rawCapture.Data.Length} {ipPacket.SourceAddress}:{tcpPacket.SourcePort}->{ipPacket.DestinationAddress}:{tcpPacket.DestinationPort}");

                            List<string> packetLines = new List<string>(packetData.Split('\n'));
                            string path = packetLines.First(x => x.Contains("GET")).Split(' ')[1].Trim();
                            string agent = packetLines.First(x => x.Contains("User-Agent")).Split(' ')[1].Replace("\r", string.Empty).Trim();
                            string accept = packetLines.First(x => x.Contains("Accept")).Split(' ')[1].Replace("\r", string.Empty).Trim();
                            string filename = Regex.Match(path, @"[a-zA-Z0-9]*\.flac").Value;

                            if (File.Exists(filename))
                            {
                                Console.WriteLine($"{filename} exists. Skipping file.");
                                continue;
                            }

                            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, path);
                            message.Headers.Add("User-Agent", agent);
                            message.Headers.Add("Accept", accept);

                            HttpResponseMessage response = await _client.SendAsync(message);
                            response.EnsureSuccessStatusCode();
                            await response.Content.LoadIntoBufferAsync();

                            using (FileStream file = File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                Console.WriteLine("Saving stream to file.");
                                await response.Content.CopyToAsync(file);
                                Console.WriteLine($"Finished writing {filename}");
                            }
                        }
                    }
                }catch(Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            Console.WriteLine("-- Capture stopped");

            // Print out the device statistics
            Console.WriteLine(device.Statistics.ToString());

            // Close the pcap device
            device.Close();
        }

        static void HandleCancelKeyPress(Object sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("-- Stopping capture");
            stopCapturing = true;

            // tell the handler that we are taking care of shutting down, don't
            // shut us down after we return because we need to do just a little
            // bit more processing to close the open capture device etc
            e.Cancel = true;
        }
    }
}
