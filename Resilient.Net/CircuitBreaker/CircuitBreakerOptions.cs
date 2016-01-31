using System;

namespace Resilient.Net
{
    /// <summary>
    /// A class representing options for <see cref="Resilient.Net.CircuitBreaker"/>
    /// </summary>
    public class CircuitBreakerOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Resilient.Net.CircuitBreakerOptions"/> class.
        /// </summary>
        public CircuitBreakerOptions()
        {
            ErrorThreshold = 2;
            SuccessThreshold = 2;
            InvocationTimeout = TimeSpan.FromSeconds(1);
            ResetTimeout = TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// The number of errors to receive before the breaker is tripped.
        /// </summary>
        public int ErrorThreshold { get; set; }

        /// <summary>
        /// The number of successful calls in a half-open state required to close the circuit.
        /// </summary>
        public int SuccessThreshold { get; set; }

        /// <summary>
        /// The amount of time to wait for the call to succeed before timing out.
        /// </summary>
        public TimeSpan InvocationTimeout { get; set; }

        /// <summary>
        /// The amount of time to wait when the circuit is open before transitioning to half-open.
        /// </summary>
        public TimeSpan ResetTimeout { get; set; }
    }
}
