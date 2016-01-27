using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resilient.Net
{
    internal abstract class BaseCircuitBreakerState : CircuitBreakerState
    {
        public CircuitBreakerSwitch Switch { get; private set; }

        public CircuitBreakerInvoker Invoker { get; private set; }

        protected BaseCircuitBreakerState(CircuitBreakerSwitch breakerSwitch, CircuitBreakerInvoker invoker)
        {
            Switch = breakerSwitch.ValueOrThrow("breakerSwitch");
            Invoker = invoker.ValueOrThrow("invoker");
        }

        public abstract T Invoke<T>(Func<T> function);
        public abstract void BecomeActive();
        public abstract void ExecutionSucceeded();
        public abstract void ExecutionFailed();
    }
}
