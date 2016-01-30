using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resilient.Net
{
	/// <summary>
	/// A base class for CircuitBreakers
	/// </summary>
	public class CircuitBreaker : CircuitBreakerSwitch, IDisposable
	{
		private readonly ClosedCircuitBreakerState _closedState;
		private readonly OpenCircuitBreakerState _openState;
		private readonly HalfOpenCircuitBreakerState _halfOpenState;
		private CircuitBreakerState _currentState;

		internal CircuitBreakerState CurrentState { get { return _currentState; } }

		/// <summary>
		/// Gets a value indicating whether this instance is closed.
		/// </summary>
		/// <value><c>true</c> if this instance is closed; otherwise, <c>false</c>.</value>
		public bool IsClosed { get { return _currentState == _closedState; } }

		/// <summary>
		/// Gets a value indicating whether this instance is open.
		/// </summary>
		/// <value><c>true</c> if this instance is open; otherwise, <c>false</c>.</value>
		public bool IsOpen { get { return _currentState == _openState; } }

		/// <summary>
		/// Gets a value indicating whether this instance is half open.
		/// </summary>
		/// <value><c>true</c> if this instance is half open; otherwise, <c>false</c>.</value>
		public bool IsHalfOpen { get { return _currentState == _halfOpenState; } }

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
			var invoker = new CircuitBreakerInvoker(scheduler);

			_closedState = new ClosedCircuitBreakerState(this, invoker, options.ErrorThreshold, options.InvocationTimeout);
			_openState = new OpenCircuitBreakerState(this, invoker, options.ResetTimeout);
			_halfOpenState = new HalfOpenCircuitBreakerState(this, invoker, options.SuccessThreshold, options.InvocationTimeout);

			_currentState = _closedState;
		}

		/// <summary>
		/// Execute the specified function.
		/// </summary>
		/// <param name="function">The function to execute if the circuit is not open.</param>
		/// <typeparam name="T">The return type of the function.</typeparam>
		public T Execute<T>(Func<T> function)
		{
			return CurrentState.Invoke(function);
		}

		#region [CircuitBreakerSwitch Implementation]

		/// <summary>
		/// Reset the breaker from the <paramref name="fromState"/> to closed.
		/// </summary>
		/// <param name="fromState">The state to transition from</param>
		public void Reset(CircuitBreakerState fromState)
		{
			Transition(fromState, _closedState);
		}

		/// <summary>
		/// Trip the circuit from <paramref name="fromState"/> to open.
		/// </summary>
		/// <param name="fromState">The state to transition from</param>
		public void Trip(CircuitBreakerState fromState)
		{
			Transition(fromState, _openState);
		}

		/// <summary>
		/// Transition from <paramref name="fromState"/> to half-open.
		/// </summary>
		/// <param name="fromState">The state to transition from</param>
		public void Try(CircuitBreakerState fromState)
		{
			Transition(fromState, _halfOpenState);
		}

		private void Transition(CircuitBreakerState fromState, CircuitBreakerState toState)
		{
			if (Interlocked.CompareExchange(ref _currentState, toState, fromState) == fromState)
			{
				toState.BecomeActive();
			}
		}

		#endregion

		#region [IDisposable Implementation]

		/// <summary>
		/// Releases all resource used by the <see cref="Resilient.Net.CircuitBreaker"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="Resilient.Net.CircuitBreaker"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="Resilient.Net.CircuitBreaker"/> in an unusable state. After
		/// calling <see cref="Dispose"/>, you must release all references to the <see cref="Resilient.Net.CircuitBreaker"/>
		/// so the garbage collector can reclaim the memory that the <see cref="Resilient.Net.CircuitBreaker"/> was occupying.</remarks>
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
				_closedState.Dispose();
				_openState.Dispose();
				_halfOpenState.Dispose();
			}
		}

		#endregion
	}
}
