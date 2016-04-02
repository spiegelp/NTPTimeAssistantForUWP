using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Networking;
using Windows.Networking.Sockets;

namespace NTPTimeAssistantForUWP.Time
{
    public class NTPClient : IDisposable
    {
        private const string DefaultNtpServer = "pool.ntp.org";
        private const int NtpDataLength = 48;
        private const int NtpPort = 123;

        public delegate void TimeResponseCallback(DateTimeOffset time);

        private DatagramSocket m_datagramSocket;
        private TimeResponseCallback m_callback;

        public NTPClient()
        {
            m_datagramSocket = null;
            m_callback = null;
        }

        public async Task RequestTimeAsync(TimeResponseCallback callback)
        {
            await RequestTimeAsync(DefaultNtpServer, callback);
        }

        public async Task RequestTimeAsync(string ntpServer, TimeResponseCallback callback)
        {
            if (string.IsNullOrWhiteSpace(ntpServer))
            {
                ntpServer = DefaultNtpServer;
            }

            m_callback = callback;

            try
            {
                byte[] ntpData = new byte[NtpDataLength];
                ntpData[0] = 0x1B; // LeapIndicator = 0 (no warning), VersionNum = 3 (IPv4 only), Mode = 3 (Client Mode)

                HostName hostname = new HostName(ntpServer);

                m_datagramSocket = new DatagramSocket();
                m_datagramSocket.Control.DontFragment = true;
                m_datagramSocket.MessageReceived += MessageReceived;

                await m_datagramSocket.ConnectAsync(hostname, NtpPort.ToString());

                using (Stream stream = m_datagramSocket.OutputStream.AsStreamForWrite(NtpDataLength))
                {
                    stream.Write(ntpData, 0, NtpDataLength);
                    stream.Flush();
                }
            }
            catch (Exception)
            {
                DisposeDatagramSocket();

                throw;
            }
        }

        public void MessageReceived(DatagramSocket socket, DatagramSocketMessageReceivedEventArgs args)
        {
            try
            {
                using (Stream stream = args.GetDataStream().AsStreamForRead())
                {
                    byte[] ntpData = new byte[NtpDataLength];
                    int i = 0;
                    int readByte = -1;

                    while ((readByte = stream.ReadByte()) > -1 && i < NtpDataLength)
                    {
                        ntpData[i] = (byte)readByte;

                        i++;
                    }

                    ulong intPart = (ulong)ntpData[40] << 24 | (ulong)ntpData[41] << 16 | (ulong)ntpData[42] << 8 | (ulong)ntpData[43];
                    ulong fractPart = (ulong)ntpData[44] << 24 | (ulong)ntpData[45] << 16 | (ulong)ntpData[46] << 8 | (ulong)ntpData[47];

                    ulong milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
                    DateTimeOffset ntpTime = new DateTimeOffset(1900, 1, 1, 0, 0, 0, 0, TimeSpan.Zero).AddMilliseconds(milliseconds);

                    if (m_callback != null)
                    {
                        m_callback(ntpTime);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                DisposeDatagramSocket();
            }
        }

        private void DisposeDatagramSocket()
        {
            if (m_datagramSocket != null)
            {
                m_datagramSocket.Dispose();
            }
        }

        public void Dispose()
        {
            DisposeDatagramSocket();

            m_callback = null;
        }
    }
}
