using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resilient.Net
{
	/// <summary>
	/// An exception that gets raised when a invocation time's out
	/// </summary>
	[Serializable]
	public class CircuitBreakerTimeoutException : Exception
	{
	}
}
