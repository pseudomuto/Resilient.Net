using System;

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
