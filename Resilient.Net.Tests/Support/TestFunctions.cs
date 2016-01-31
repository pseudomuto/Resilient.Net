using System;
using System.Threading;

namespace Resilient.Net.Tests
{
    public static class TestFunctions
    {
        public static Func<string> NotImplemented = () =>
        {
            throw new NotImplementedException();
        };

        public static Func<T> Delay<T>(int milliseconds, Func<T> function)
        {
            return () =>
            {
                Thread.Sleep(milliseconds);
                return function();
            };            
        }
    }
}

