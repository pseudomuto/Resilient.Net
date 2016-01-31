using System;

namespace Resilient.Net
{
    /// <summary>
    /// A class representing options for <see cref="Resilient.Net.CircuitBreaker"/>
    /// </summary>
    public class CircuitBreakerOptions
    {
        private static readonly string s_logFormat = string.Join(
                                                         " ",
                                                         "ErrorThreshold={0}",
                                                         "SuccessThreshold={1}",
                                                         "InvocationTimeout={2}",
                                                         "ResetTimeout={3}"
                                                     );

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

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current 
        /// <see cref="Resilient.Net.CircuitBreakerOptions"/>.
        /// </summary>
        /// <remarks>This is intended for logging purposes</remarks>
        /// <returns>
        /// A <see cref="System.String"/> that represents the current <see cref="Resilient.Net.CircuitBreakerOptions"/>.
        /// </returns>
        public override string ToString()
        {
            return string.Format(
                s_logFormat,
                ErrorThreshold,
                SuccessThreshold,
                InvocationTimeout.TotalMilliseconds,
                ResetTimeout.TotalMilliseconds
            );
        }
    }
}
