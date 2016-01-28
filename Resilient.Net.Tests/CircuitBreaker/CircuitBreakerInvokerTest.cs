using NSubstitute;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Resilient.Net.Tests
{
    public class CircuitBreakerInvokerTest
    {
        public class Constructor
        {
            [Fact]
            public void WhenGivenAValidSchedule()
            {
                new CircuitBreakerInvoker(TaskScheduler.Current);
            }

            [Fact]
            public void WhenGivenANullTaskScheduler()
            {
                Assert.Throws<ArgumentNullException>(() => new CircuitBreakerInvoker(null));
            }
        }

        public class Invoke
        {
            private readonly CircuitBreakerInvoker _invoker = new CircuitBreakerInvoker(TaskScheduler.Default);
            private readonly CircuitBreakerState _mock = Substitute.For<CircuitBreakerState>();
            
            [Fact]
            public void WhenSuccessfulReturnsTheResultAndNotifiesState()
            {
                var expected = "Some value";
                var result = _invoker.Invoke(_mock, () => expected, TimeSpan.FromSeconds(1));

                _mock.Received().ExecutionSucceeded();
                Assert.Equal(expected, result);
            }

            [Fact]
            public void WhenFailedReturnsTheDefaultResultAndNotifiesState()
            {
                Func<string> cmd = () => { throw new Exception("Boom-Shakalaka"); };
                
                try
                {
                    _invoker.Invoke(_mock, cmd, TimeSpan.FromSeconds(1));
                    Assert.True(false, "Shouldn't have made it here");                    
                }
                catch (Exception exception)
                {
                    Assert.Equal("Boom-Shakalaka", exception.Message);
                    _mock.Received().ExecutionFailed();
                }
            }
        }
    }
}
