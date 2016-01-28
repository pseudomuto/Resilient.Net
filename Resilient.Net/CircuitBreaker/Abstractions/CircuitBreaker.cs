using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resilient.Net
{
    public abstract class CircuitBreaker<T> : CircuitBreakerSwitch, IDisposable
    {
        private readonly ClosedCircuitBreakerState _closedState;
        private readonly OpenCircuitBreakerState _openState;
        private readonly HalfOpenCircuitBreakerState _halfOpenState;
        private CircuitBreakerState _currentState;

        internal CircuitBreakerState CurrentState { get { return _currentState; } }

        public bool IsClosed { get { return _currentState == _closedState; } }
        public bool IsOpen { get { return _currentState == _openState; } }
        public bool IsHalfOpen { get { return _currentState == _halfOpenState; } }

        protected CircuitBreaker()
            : this(TaskScheduler.Default)
        {
        }

        protected CircuitBreaker(TaskScheduler scheduler)
            : this(scheduler, CircuitBreakerOptions.Default)
        {
        }

        protected CircuitBreaker(TaskScheduler scheduler, CircuitBreakerOptions options)
        {            
            var invoker = new CircuitBreakerInvoker(scheduler);

            _closedState = new ClosedCircuitBreakerState(this, invoker, options.ErrorThreshold, options.InvocationTimeout);
            _openState = new OpenCircuitBreakerState(this, invoker, options.ResetTimeout);
            _halfOpenState = new HalfOpenCircuitBreakerState(this, invoker, options.SuccessThreshold, options.InvocationTimeout);

            _currentState = _closedState;
        }

        public T Execute()
        {
            return CurrentState.Invoke(this.Perform);
        }

        protected abstract T Perform();

        #region [CircuitBreakerSwitch Implementation]

        public void Reset(CircuitBreakerState fromState)
        {
            Transition(fromState, _closedState);
        }

        public void Trip(CircuitBreakerState fromState)
        {
            Transition(fromState, _openState);
        }

        public void Try(CircuitBreakerState fromState)
        {
            Transition(fromState, _halfOpenState);
        }

        private void Transition(CircuitBreakerState fromState, CircuitBreakerState toState)
        {
            if(Interlocked.CompareExchange(ref _currentState, toState, fromState) == fromState)
            {
                toState.BecomeActive();
            }
        }

        #endregion

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _closedState.Dispose();
                _openState.Dispose();
                _halfOpenState.Dispose();
            }
        }
    }
}
