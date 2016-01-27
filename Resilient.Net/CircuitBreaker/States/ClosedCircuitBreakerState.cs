using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resilient.Net
{
    internal class ClosedCircuitBreakerState :BaseCircuitBreakerState
    {
        private readonly int _errorThreshold;
        private readonly TimeSpan _invocationTimeout;

        private int _failures = 0;

        public int Failures {  get { return _failures; } }

        public ClosedCircuitBreakerState(CircuitBreakerSwitch breakerSwitch, CircuitBreakerInvoker invoker, int errorThreshold, TimeSpan invocationTimeout)
            : base(breakerSwitch, invoker)
        {
            _errorThreshold = errorThreshold.PositiveValueOrThrow("errorThreshold");
            _invocationTimeout = invocationTimeout.PositiveOrThrow("invocationTimeout");
        }

        public override T Invoke<T>(Func<T> function)
        {
            return Invoker.Invoke(this, function, _invocationTimeout);
        }

        public override void ExecutionSucceeded()
        {
            Interlocked.Exchange(ref _failures, 0);
        }

        public override void ExecutionFailed()
        {
            if (Interlocked.Increment(ref _failures) == _errorThreshold)
            {
                Switch.Trip(this);
            }
        }

        public override void BecomeActive()
        {
            Interlocked.Exchange(ref _failures, 0);
        }
    }
}
