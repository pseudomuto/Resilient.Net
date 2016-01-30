using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resilient.Net
{
	/// <summary>
	/// Circuit breaker switch.
	/// </summary>
	public interface CircuitBreakerSwitch
	{
		/// <summary>
		/// Reset the breaker from the <paramref name="fromState"/> to closed.
		/// </summary>
		/// <param name="fromState">The state to transition from</param>
		void Reset(CircuitBreakerState fromState);

		/// <summary>
		/// Trip the circuit from <paramref name="fromState"/> to open.
		/// </summary>
		/// <param name="fromState">The state to transition from</param>
		void Trip(CircuitBreakerState fromState);

		/// <summary>
		/// Transition from <paramref name="fromState"/> to half-open.
		/// </summary>
		/// <param name="fromState">The state to transition from</param>
		void Try(CircuitBreakerState fromState);
	}
}
