using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resilient.Net
{
    public interface CircuitBreakerSwitch
    {
        void Reset(CircuitBreakerState fromState);
        void Trip(CircuitBreakerState fromState);
        void Try(CircuitBreakerState fromState);
    }
}
