using System;
using Xunit;

namespace Resilient.Net.Tests
{
    public static class CircuitBreakerOptionsTest
    {
        public class Defaults
        {
            private readonly CircuitBreakerOptions _options = new CircuitBreakerOptions();

            [Fact]
            public void ErrorThreshold()
            {
                Assert.Equal(2, _options.ErrorThreshold);
            }

            [Fact]
            public void SuccessTheshold()
            {
                Assert.Equal(2, _options.SuccessThreshold);
            }

            [Fact]
            public void InvocationTimeout()
            {
                Assert.Equal(TimeSpan.FromSeconds(1), _options.InvocationTimeout);
            }

            [Fact]
            public void ResetTimeout()
            {
                Assert.Equal(TimeSpan.FromSeconds(10), _options.ResetTimeout);
            }
        }

        public class ToStringOverride
        {
            private readonly CircuitBreakerOptions _options = new CircuitBreakerOptions();

            [Fact]
            public void IncludesOptionDetails()
            {
                var expected = "ErrorThreshold=2 SuccessThreshold=2 InvocationTimeout=1000 ResetTimeout=10000";
                Assert.Equal(expected, _options.ToString());
            }
        }
    }
}

