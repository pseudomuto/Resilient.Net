using System;

namespace Resilient.Net.Tests
{
    public abstract class CircuitBreakerStateTest<T> : IDisposable where T : CircuitBreakerState
    {
        protected T _state;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                var disposable = _state as IDisposable;

                if (disposable != null)
                {
                    disposable.Dispose();
                }                
            }
        }
    }
}
