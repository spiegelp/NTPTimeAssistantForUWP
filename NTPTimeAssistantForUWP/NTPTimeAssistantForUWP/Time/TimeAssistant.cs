using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTPTimeAssistantForUWP.Time
{
    /// <summary>
    /// Controller to manage NTP requests and to create DateTime and DateTimeOffset instances based on the response.
    /// A call to SynchronizeClockAsync() is needed to initialize the TimeAssistant, which caches the offset between the hardware time and the NTP time.
    /// </summary>
    public class TimeAssistant
    {
        private static object m_staticLockObject = new object();
        private object m_lockObject = new object();

        public const int RequestTimeout = 5000; // in ms
        private const int WaitingCycleDuration = 100; // in ms

        private static TimeAssistant m_instance;

        public event TimeSynchronizedEventHandler TimeSynchronized;

        private TimeSpan m_offset;
        private bool m_isSynchronizing;
        private NTPClient m_ntpClient;

        /// <summary>
        /// Singleton factory for the TimeAssistant.
        /// </summary>
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

        /// <summary>
        /// The current time.
        /// </summary>
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

        /// <summary>
        /// The current time with UTC as the time zone.
        /// </summary>
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

        /// <summary>
        /// The current time.
        /// </summary>
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

        /// <summary>
        /// The current time with UTC as the time zone.
        /// </summary>
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

        /// <summary>
        /// True, if the TimeAssistant is synchronizing it's time with a NTP server.
        /// </summary>
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

        /// <summary>
        /// Synchronizes the time with the default NTP server (pool.ntp.org) and quits the process if it is not finished after 5000 milliseconds.
        /// A DateTimeOffset instance in UTC of the current timestamp will be return after a successful request.
        /// Calls during a synchronization process will return null.
        /// </summary>
        /// <returns>the currrent time or throws an exception on timeout</returns>
        public async Task<DateTimeOffset?> SynchronizeTimeAsync()
        {
            return await SynchronizeTimeAsync(true);
        }

        /// <summary>
        /// Synchronizes the time with the default NTP server (pool.ntp.org) and quits the process if it is not finished after 5000 milliseconds.
        /// A DateTimeOffset instance in UTC of the current timestamp will be return after a successful request.
        /// Calls during a synchronization process will return null.
        /// </summary>
        /// <returns>the currrent time or on timeout optionally null or an exception</returns>
        public async Task<DateTimeOffset?> SynchronizeTimeAsync(bool throwExceptionOnTimeout)
        {
            return await DoSynchronizeTimeAsync(null, RequestTimeout, throwExceptionOnTimeout);
        }

        /// <summary>
        /// Synchronizes the time with the provided NTP server and quits the process if it is not finished after 5000 milliseconds.
        /// A DateTimeOffset instance in UTC of the current timestamp will be return after a successful request.
        /// Calls during a synchronization process will return null.
        /// </summary>
        /// <param name="ntpServer"></param>
        /// <returns>the currrent time or throws an exception on timeout</returns>
        public async Task<DateTimeOffset?> SynchronizeTimeAsync(string ntpServer)
        {
            return await SynchronizeTimeAsync(ntpServer, true);
        }

        /// <summary>
        /// Synchronizes the time with the provided NTP server and quits the process if it is not finished after 5000 milliseconds.
        /// A DateTimeOffset instance in UTC of the current timestamp will be return after a successful request.
        /// Calls during a synchronization process will return null.
        /// </summary>
        /// <param name="ntpServer"></param>
        /// <returns>the currrent time or on timeout optionally null or an exception</returns>
        public async Task<DateTimeOffset?> SynchronizeTimeAsync(string ntpServer, bool throwExceptionOnTimeout)
        {
            return await DoSynchronizeTimeAsync(ntpServer, RequestTimeout, throwExceptionOnTimeout);
        }

        /// <summary>
        /// Synchronizes the time with the default NTP server (pool.ntp.org) and quits the process if it is not finished after requestTimeout milliseconds.
        /// A DateTimeOffset instance in UTC of the current timestamp will be return after a successful request.
        /// Calls during a synchronization process will return null.
        /// </summary>
        /// <param name="requestTimeout"></param>
        /// <returns>the currrent time or throws an exception on timeout</returns>
        public async Task<DateTimeOffset?> SynchronizeTimeAsync(int requestTimeout)
        {
            return await SynchronizeTimeAsync(requestTimeout, true);
        }

        /// <summary>
        /// Synchronizes the time with the default NTP server (pool.ntp.org) and quits the process if it is not finished after requestTimeout milliseconds.
        /// A DateTimeOffset instance in UTC of the current timestamp will be return after a successful request.
        /// Calls during a synchronization process will return null.
        /// </summary>
        /// <param name="requestTimeout"></param>
        /// <returns>the currrent time or on timeout optionally null or an exception</returns>
        public async Task<DateTimeOffset?> SynchronizeTimeAsync(int requestTimeout, bool throwExceptionOnTimeout)
        {
            return await DoSynchronizeTimeAsync(null, requestTimeout, throwExceptionOnTimeout);
        }

        /// <summary>
        /// Synchronizes the time with the provided NTP server and quits the process if it is not finished after requestTimeout milliseconds.
        /// A DateTimeOffset instance in UTC of the current timestamp will be return after a successful request.
        /// Calls during a synchronization process will return null.
        /// </summary>
        /// <param name="ntpServer"></param>
        /// <param name="requestTimeout"></param>
        /// <returns>the currrent time or throws an exception on timeout</returns>
        public async Task<DateTimeOffset?> SynchronizeTimeAsync(string ntpServer, int requestTimeout)
        {
            return await SynchronizeTimeAsync(ntpServer, requestTimeout, true);
        }

        /// <summary>
        /// Synchronizes the time with the provided NTP server and quits the process if it is not finished after requestTimeout milliseconds.
        /// A DateTimeOffset instance in UTC of the current timestamp will be return after a successful request.
        /// Calls during a synchronization process will return null.
        /// </summary>
        /// <param name="ntpServer"></param>
        /// <param name="requestTimeout"></param>
        /// <returns>the currrent time or on timeout optionally null or an exception</returns>
        public async Task<DateTimeOffset?> SynchronizeTimeAsync(string ntpServer, int requestTimeout, bool throwExceptionOnTimeout)
        {
            return await DoSynchronizeTimeAsync(ntpServer, requestTimeout, throwExceptionOnTimeout);
        }

        private async Task<DateTimeOffset?> DoSynchronizeTimeAsync(string ntpServer, int requestTimeout, bool throwExceptionOnTimeout)
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

                    // send the request
                    m_ntpClient = new NTPClient();
                    await m_ntpClient.RequestTimeAsync(ntpServer, TimeResponseCallback);

                    // wait for requestTimeout milliseconds for a response
                    int numberOfWaitingCycles = RequestTimeout / WaitingCycleDuration;
                    int waitingCycle = 0;

                    while (IsSynchronizing && waitingCycle < numberOfWaitingCycles)
                    {
                        waitingCycle++;
                        await Task.Delay(WaitingCycleDuration);
                    }

                    if (!IsSynchronizing)
                    {
                        // the client got a response and called the TimeResponseCallback
                        //     -> return a DateTimeOffset instance in UTC of the current timestamp
                        return DateTimeOffsetUtc;
                    }
                    else
                    {
                        if (throwExceptionOnTimeout)
                        {
                            throw new TimeoutException();
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    DisposeNTPClient();

                    // stop the synchronizing process
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
                // remember the offset between the hardware time and the NTP time
                m_offset = time - DateTimeOffset.UtcNow;

                // stop the synchronizing process
                IsSynchronizing = false;
            }

            OnTimeSynchronized(DateTimeOffsetUtc);
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

        private void OnTimeSynchronized(DateTimeOffset synchronizedTimeUtc)
        {
            if (TimeSynchronized != null)
            {
                TimeSynchronized(this, new TimeSynchronizedEventArgs(synchronizedTimeUtc));
            }
        }
    }
}
