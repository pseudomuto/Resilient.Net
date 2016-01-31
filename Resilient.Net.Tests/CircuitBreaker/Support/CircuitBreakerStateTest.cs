using System;

namespace Resilient.Net.Tests
{
    public abstract class CircuitBreakerStateTest : IDisposable
    {
        protected CircuitBreakerState State { get; set; }

        public T As<T>() where T : class, CircuitBreakerState
        {
            return State as T;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                var disposable = State as IDisposable;

                if (disposable != null)
                {
                    disposable.Dispose();
                }                
            }
        }
    }
}
