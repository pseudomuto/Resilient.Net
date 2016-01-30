using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resilient.Net
{
	/// <summary>
	/// An exception that gets raised when a circuit is open
	/// </summary>
	[Serializable]
	public class OpenCircuitBreakerException : Exception
	{
	}
}
