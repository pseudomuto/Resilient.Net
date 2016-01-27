using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resilient.Net
{
    public interface CircuitBreakerState
    {
        T Invoke<T>(Func<T> function);

        void BecomeActive();
        void ExecutionSucceeded();
        void ExecutionFailed();        
    }
}
