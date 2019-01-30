using Microsoft.Extensions.Logging;
using PacketDotNet;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BugsSniffer.Api
{
    public class PacketExtractor
    {
        private readonly ILogger<PacketExtractor> _logger;

        public PacketExtractor(ILoggerFactory factory)
        {
            _logger = factory.CreateLogger<PacketExtractor>();
        }

        public (string path, string agent, string accept) Extract(RawCapture capture)
        {
            try
            {
                string path = null;
                string agent = null;
                string accept = null;
                // use PacketDotNet to parse this packet and print out
                // its high level information
                Packet p = Packet.ParsePacket(capture.LinkLayerType, capture.Data);

                var tcpPacket = (TcpPacket)p.Extract(typeof(TcpPacket));
                if (tcpPacket != null)
                {
                    string packetData = tcpPacket.PayloadData != null ? Encoding.ASCII.GetString(tcpPacket.PayloadData) : "Empty";

                    if (packetData.Contains("GET") && packetData.Contains("bp-aod.bugs.gscdn.com"))
                    {
                        IpPacket ipPacket = (IpPacket)tcpPacket.ParentPacket;
                        _logger.LogInformation($"{capture.Timeval:hh:mm:ss,fff} Len={capture.Data.Length} {ipPacket.SourceAddress}:{tcpPacket.SourcePort}->{ipPacket.DestinationAddress}:{tcpPacket.DestinationPort}");

                        List<string> packetLines = new List<string>(packetData.Split('\n'));
                        path = packetLines.First(x => x.Contains("GET")).Split(' ')[1].Trim();
                        agent = packetLines.First(x => x.Contains("User-Agent")).Split(' ')[1].Replace("\r", string.Empty).Trim();
                        accept = packetLines.First(x => x.Contains("Accept")).Split(' ')[1].Replace("\r", string.Empty).Trim();
                    }
                }

                return (path, agent, accept);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error occured during extraction of packet.");
                return (null, null, null);
            }
        }
    }
}
