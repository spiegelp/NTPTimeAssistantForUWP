using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTPTimeAssistantForUWP.Time
{
    public class TimeSynchronizedEventArgs : EventArgs
    {
        private DateTimeOffset m_synchronizedTimeUtc;

        public DateTimeOffset SynchronizedTimeUtc
        {
            get
            {
                return m_synchronizedTimeUtc;
            }
        }

        public TimeSynchronizedEventArgs(DateTimeOffset synchronizedTimeUtc)
            : base()
        {
            m_synchronizedTimeUtc = synchronizedTimeUtc;
        }
    }

    public delegate void TimeSynchronizedEventHandler(object sender, TimeSynchronizedEventArgs args);
}
