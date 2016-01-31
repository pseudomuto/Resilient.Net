﻿using System;

namespace Resilient.Net
{
    internal abstract class BaseCircuitBreakerState : CircuitBreakerState, IDisposable
    {
        public CircuitBreakerSwitch Switch { get; private set; }

        public CircuitBreakerInvoker Invoker { get; private set; }

        protected BaseCircuitBreakerState(CircuitBreakerSwitch breakerSwitch, CircuitBreakerInvoker invoker)
        {
            Switch = breakerSwitch.ValueOrThrow("breakerSwitch");
            Invoker = invoker.ValueOrThrow("invoker");
        }

        public abstract T Invoke<T>(Func<T> function);

        public abstract CircuitBreakerStateType Type { get; }

        public abstract void BecomeActive();

        public abstract void ExecutionSucceeded();

        public abstract void ExecutionFailed();

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
