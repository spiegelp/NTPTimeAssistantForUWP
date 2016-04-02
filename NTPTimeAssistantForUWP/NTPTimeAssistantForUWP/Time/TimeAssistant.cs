using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTPTimeAssistantForUWP.Time
{
    public class TimeAssistant
    {
        private static object m_staticLockObject = new object();
        private object m_lockObject = new object();

        public const int RequestTimeout = 5000; // in ms
        private const int WaitingCycleDuration = 100; // in ms

        private static TimeAssistant m_instance;

        public event ClockSynchronizedEventHandler ClockSynchronized;

        private TimeSpan m_offset;
        private bool m_isSynchronizing;
        private NTPClient m_ntpClient;

        public static TimeAssistant Instance
        {
            get
            {
                lock (m_staticLockObject)
                {
                    if (m_instance == null)
                    {
                        m_instance = new TimeAssistant();
                    }

                    return m_instance;
                }
            }
        }

        public DateTime DateTime
        {
            get
            {
                lock (m_lockObject)
                {
                    return DateTime.Now + m_offset;
                }
            }
        }

        public DateTime DateTimeUtc
        {
            get
            {
                lock (m_lockObject)
                {
                    return DateTime.UtcNow + m_offset;
                }
            }
        }

        public DateTimeOffset DateTimeOffset
        {
            get
            {
                lock (m_lockObject)
                {
                    return DateTimeOffset.Now + m_offset;
                }
            }
        }

        public DateTimeOffset DateTimeOffsetUtc
        {
            get
            {
                lock (m_lockObject)
                {
                    return DateTimeOffset.UtcNow + m_offset;
                }
            }
        }

        public bool IsSynchronizing
        {
            get
            {
                lock (m_lockObject)
                {
                    return m_isSynchronizing;
                }
            }

            private set
            {
                m_isSynchronizing = value;
            }
        }

        private TimeAssistant()
        {
            m_offset = TimeSpan.Zero;
            m_isSynchronizing = false;
            m_ntpClient = null;
        }

        public async Task<DateTimeOffset?> SynchronizeClockAsync()
        {
            return await DoSynchronizeClockAsync(null, RequestTimeout);
        }

        public async Task<DateTimeOffset?> SynchronizeClockAsync(string ntpServer)
        {
            return await DoSynchronizeClockAsync(ntpServer, RequestTimeout);
        }

        public async Task<DateTimeOffset?> SynchronizeClockAsync(int requestTimeout)
        {
            return await DoSynchronizeClockAsync(null, requestTimeout);
        }

        public async Task<DateTimeOffset?> SynchronizeClockAsync(string ntpServer, int requestTimeout)
        {
            return await DoSynchronizeClockAsync(ntpServer, requestTimeout);
        }

        private async Task<DateTimeOffset?> DoSynchronizeClockAsync(string ntpServer, int requestTimeout)
        {
            bool doSynchronize = false;

            lock (m_lockObject)
            {
                if (!IsSynchronizing)
                {
                    IsSynchronizing = true;
                    doSynchronize = true;
                }
            }

            if (doSynchronize)
            {
                try
                {
                    if (requestTimeout < RequestTimeout)
                    {
                        requestTimeout = RequestTimeout;
                    }

                    m_ntpClient = new NTPClient();
                    await m_ntpClient.RequestTimeAsync(ntpServer, TimeResponseCallback);

                    int numberOfWaitingCycles = RequestTimeout / WaitingCycleDuration;
                    int waitingCycle = 0;

                    while (IsSynchronizing && waitingCycle < numberOfWaitingCycles)
                    {
                        waitingCycle++;
                        await Task.Delay(WaitingCycleDuration);
                    }

                    if (!IsSynchronizing)
                    {
                        return DateTimeOffsetUtc;
                    }
                    else
                    {
                        throw new TimeoutException();
                    }
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    DisposeNTPClient();
                    IsSynchronizing = false;
                }
            }
            else
            {
                return null;
            }
        }

        private void TimeResponseCallback(DateTimeOffset time)
        {
            lock (m_lockObject)
            {
                m_offset = time - DateTimeOffset.UtcNow;
                IsSynchronizing = false;
            }

            OnClockSynchronized(DateTimeOffsetUtc);
        }

        private void DisposeNTPClient()
        {
            lock (m_lockObject)
            {
                if (m_ntpClient != null)
                {
                    m_ntpClient.Dispose();
                }
            }
        }

        private void OnClockSynchronized(DateTimeOffset synchronizedTimeUtc)
        {
            if (ClockSynchronized != null)
            {
                ClockSynchronized(this, new ClockSynchronizedEventArgs(synchronizedTimeUtc));
            }
        }
    }
}
