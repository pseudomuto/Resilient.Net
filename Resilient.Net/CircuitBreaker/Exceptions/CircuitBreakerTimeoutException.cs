using System;

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
