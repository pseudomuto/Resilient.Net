using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resilient.Net
{
    /// <summary>
    /// Enumeration of available circuit breaker states types
    /// </summary>
    public enum CircuitBreakerStateType
    {
        Closed = 0,
        Open = 1,
        HalfOpen = 2
    }

    /// <summary>
    /// Circuit breaker state.
    /// </summary>
    public interface CircuitBreakerState
    {
        /// <summary>
        /// Invokes the specified function.
        /// </summary>
        /// <param name="function">The function to invoke</param>
        /// <typeparam name="T">The return type of the function.</typeparam>
        T Invoke<T>(Func<T> function);

        /// <summary>
        /// Code to run when this state becomes active. This will be called when this state is transitioned to.
        /// </summary>
        void BecomeActive();

        /// <summary>
        /// What to do when an invocation succeeds.
        /// </summary>
        void ExecutionSucceeded();

        /// <summary>
        /// What to do when an invocation fails.
        /// </summary>
        void ExecutionFailed();
    }
}
