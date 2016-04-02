using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTPTimeAssistantForUWP.Time
{
    public class ClockSynchronizedEventArgs : EventArgs
    {
        private DateTimeOffset m_synchronizedTimeUtc;

        public DateTimeOffset SynchronizedTimeUtc
        {
            get
            {
                return m_synchronizedTimeUtc;
            }
        }

        public ClockSynchronizedEventArgs(DateTimeOffset synchronizedTimeUtc)
            : base()
        {
            m_synchronizedTimeUtc = synchronizedTimeUtc;
        }
    }

    public delegate void ClockSynchronizedEventHandler(object sender, ClockSynchronizedEventArgs args);
}
