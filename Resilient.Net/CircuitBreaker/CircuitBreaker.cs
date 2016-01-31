using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Resilient.Net
{
    /// <summary>
    /// A base class for CircuitBreakers
    /// </summary>
    public class CircuitBreaker : CircuitBreakerSwitch, IDisposable
    {
        private readonly CircuitBreakerInvoker _invoker;
        private readonly CircuitBreakerOptions _options;
        private readonly CircuitBreakerState[] _states;

        private CircuitBreakerState _currentState;

        /// <summary>
        /// Gets a value indicating whether this instance is closed.
        /// </summary>
        /// <value><c>true</c> if this instance is closed; otherwise, <c>false</c>.</value>
        public bool IsClosed { get { return _currentState == GetStateForType(CircuitBreakerStateType.Closed); } }

        /// <summary>
        /// Gets a value indicating whether this instance is open.
        /// </summary>
        /// <value><c>true</c> if this instance is open; otherwise, <c>false</c>.</value>
        public bool IsOpen { get { return _currentState == GetStateForType(CircuitBreakerStateType.Open); } }

        /// <summary>
        /// Gets a value indicating whether this instance is half open.
        /// </summary>
        /// <value><c>true</c> if this instance is half open; otherwise, <c>false</c>.</value>
        public bool IsHalfOpen { get { return _currentState == GetStateForType(CircuitBreakerStateType.HalfOpen); } }

        /// <summary>
        /// Initializes a new instance of the <see cref="Resilient.Net.CircuitBreaker"/> class using the default
        /// <see cref="TaskScheduler"/> and <see cref="Resilient.Net.CircuitBreakerOptions"/>.
        /// </summary>
        public CircuitBreaker()
            : this(TaskScheduler.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Resilient.Net.CircuitBreaker"/> class using the specified
        /// <see cref="TaskScheduler"/> and default <see cref="Resilient.Net.CircuitBreakerOptions"/>.
        /// </summary>
        /// <param name="scheduler">The <see cref="TaskScheduler"/> to use for execution.</param>
        public CircuitBreaker(TaskScheduler scheduler)
            : this(scheduler, new CircuitBreakerOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Resilient.Net.CircuitBreaker"/> class.
        /// </summary>
        /// <param name="scheduler">The <see cref="TaskScheduler"/> to use for execution.</param>
        /// <param name="options">The <see cref="Resilient.Net.CircuitBreakerOptions"/> to use</param>
        public CircuitBreaker(TaskScheduler scheduler, CircuitBreakerOptions options)
        {
            _invoker = new CircuitBreakerInvoker(scheduler);
            _options = options;

            _states = new CircuitBreakerState[] {
                MakeStateForType(CircuitBreakerStateType.Closed, _options),
                MakeStateForType(CircuitBreakerStateType.HalfOpen, _options),
                MakeStateForType(CircuitBreakerStateType.Open, _options)
            };

            _currentState = GetStateForType(CircuitBreakerStateType.Closed);
        }

        /// <summary>
        /// Execute the specified action.
        /// </summary>
        /// <param name="action">The action to run if the circuit is not open.</param>
        public void Execute(Action action)
        {
            _currentState.Invoke(() =>
            {
                action.Invoke();
                return true;
            });
        }

        /// <summary>
        /// Execute the specified function.
        /// </summary>
        /// <param name="function">The function to execute if the circuit is not open.</param>
        /// <typeparam name="T">The return type of the function.</typeparam>
        public T Execute<T>(Func<T> function)
        {
            return _currentState.Invoke(function);
        }

        #region [CircuitBreakerSwitch Implementation]

        /// <summary>
        /// Reset the breaker from the <paramref name="fromState"/> to closed.
        /// </summary>
        /// <param name="fromState">The state to transition from</param>
        public void Reset(CircuitBreakerState fromState)
        {
            Transition(fromState, GetStateForType(CircuitBreakerStateType.Closed));
        }

        /// <summary>
        /// Trip the circuit from <paramref name="fromState"/> to open.
        /// </summary>
        /// <param name="fromState">The state to transition from</param>
        public void Trip(CircuitBreakerState fromState)
        {
            Transition(fromState, GetStateForType(CircuitBreakerStateType.Open));
        }

        /// <summary>
        /// Transition from <paramref name="fromState"/> to half-open.
        /// </summary>
        /// <param name="fromState">The state to transition from</param>
        public void Try(CircuitBreakerState fromState)
        {
            Transition(fromState, GetStateForType(CircuitBreakerStateType.HalfOpen));
        }

        /// <summary>
        /// Force the circuit breaker into the specified state.
        /// </summary>
        /// <param name="toState">The state to transition to.</param>
        public void Force(CircuitBreakerStateType toState)
        {
            Transition(_currentState, GetStateForType(toState));
        }

        private void Transition(CircuitBreakerState fromState, CircuitBreakerState toState)
        {
            if (Interlocked.CompareExchange(ref _currentState, toState, fromState) == fromState)
            {
                LogTransition(fromState, toState);
                toState.BecomeActive();
            }
        }

        private void LogTransition(CircuitBreakerState fromState, CircuitBreakerState toState)
        {
            Trace.TraceInformation(
                "[{0}] state transition from {1} to {2}. {3}",
                GetType().Name,
                fromState.Type,
                toState.Type,
                _options
            );
        }

        #endregion

        #region [IDisposable Implementation]

        /// <summary>
        /// Releases all resource used by the <see cref="Resilient.Net.CircuitBreaker"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the circuit breaker. The 
        /// <see cref="Dispose"/> method leaves the <see cref="Resilient.Net.CircuitBreaker"/> in an unusable state. 
        /// After calling <see cref="Dispose"/>, you must release all references to the circuit breaker so the garbage 
        /// collector can reclaim the memory that was occupying.</remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose the specified disposing.
        /// </summary>
        /// <param name="disposing">If set to <c>true</c> disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var state in _states)
                {
                    var disposable = state as IDisposable;

                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }

        #endregion

        private CircuitBreakerState MakeStateForType(CircuitBreakerStateType type, CircuitBreakerOptions options)
        {
            CircuitBreakerState state;

            switch (type)
            {
                case CircuitBreakerStateType.Open:
                    state = new OpenCircuitBreakerState(this, _invoker, options.ResetTimeout);
                    break;
                case CircuitBreakerStateType.HalfOpen:
                    state = new HalfOpenCircuitBreakerState(
                        this,
                        _invoker,
                        options.SuccessThreshold,
                        options.InvocationTimeout
                    );
                    break;
                default:
                    state = new ClosedCircuitBreakerState(
                        this,
                        _invoker,
                        options.ErrorThreshold,
                        options.InvocationTimeout
                    );
                    break;
            }

            return state;
        }

        private CircuitBreakerState GetStateForType(CircuitBreakerStateType type)
        {
            return _states.First(state => state.Type == type);
        }
    }
}
