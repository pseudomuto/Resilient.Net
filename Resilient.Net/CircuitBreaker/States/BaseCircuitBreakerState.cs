using System;

namespace Resilient.Net
{
    internal abstract class BaseCircuitBreakerState : CircuitBreakerState, IDisposable
    {
        public CircuitBreakerSwitch Switch { get; private set; }

        public CircuitBreakerInvoker Invoker { get; private set; }

        protected BaseCircuitBreakerState(CircuitBreakerSwitch @switch, CircuitBreakerInvoker invoker)
        {
            Switch = @switch.OrThrow("@switch");
            Invoker = invoker.OrThrow("invoker");
        }

        public abstract T Invoke<T>(Func<T> function);

        public abstract CircuitBreakerStateType Type { get; }

        public abstract void BecomeActive();

        public abstract void ExecutionSucceeded();

        public abstract void ExecutionFailed();

        public override string ToString()
        {
            return GetType().FullName;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
