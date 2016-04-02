using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTPTimeAssistantForUWP.Time
{
    public class TimeoutException : Exception
    {
        public TimeoutException() : base() { }

        public TimeoutException(string message) : base(message) { }
    }
}
