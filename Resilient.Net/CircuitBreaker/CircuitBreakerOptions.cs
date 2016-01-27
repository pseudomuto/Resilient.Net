using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resilient.Net
{
    public struct CircuitBreakerOptions
    {
        public static readonly CircuitBreakerOptions Default = new CircuitBreakerOptions
        {
            ErrorThreshold = 2,
            SuccessThreshold = 2,
            InvocationTimeout = TimeSpan.FromMilliseconds(1000),
            ResetTimeout = TimeSpan.FromMilliseconds(10000)
        };

        public int ErrorThreshold { get; set; }
        public int SuccessThreshold { get; set; }
        public TimeSpan InvocationTimeout { get; set; }
        public TimeSpan ResetTimeout { get; set; }
    }
}
