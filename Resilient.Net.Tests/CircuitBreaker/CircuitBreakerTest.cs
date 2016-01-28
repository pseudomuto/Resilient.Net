using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Resilient.Net.Tests
{
    public class CircuitBreakerTest
    {
        public class DummyBreaker : CircuitBreaker<string>
        {
            public Func<string> Function { get; set; }

            public DummyBreaker()
            {
            }

            public DummyBreaker(TaskScheduler scheduler) 
                : base(scheduler)
            {
            }

            public DummyBreaker(TaskScheduler scheduler, CircuitBreakerOptions options) 
                : base(scheduler, options)
            {
            }
                        
            protected override string Perform()
            {
                if (Function != null)
                {
                    return Function.Invoke();
                }

                return "Dummy String";
            }
        }

        public abstract class BreakerTest : IDisposable
        {
            protected DummyBreaker _breaker;

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _breaker.Dispose();
                }
            }
        }

        public class IsState : BreakerTest
        {            
            public IsState()
            {
                _breaker = new DummyBreaker();
            }

            [Fact]
            public void IsClosed()
            {
                Assert.True(_breaker.IsClosed);
                Assert.False(_breaker.IsOpen);
                Assert.False(_breaker.IsHalfOpen);
            }

            [Fact]
            public void IsOpen()
            {
                _breaker.Trip(_breaker.CurrentState);

                Assert.True(_breaker.IsOpen);
                Assert.False(_breaker.IsClosed);
                Assert.False(_breaker.IsHalfOpen);
            }

            [Fact]
            public void IsHalfOpen()
            {
                _breaker.Try(_breaker.CurrentState);

                Assert.True(_breaker.IsHalfOpen);
                Assert.False(_breaker.IsClosed);
                Assert.False(_breaker.IsOpen);
            }
        }

        public class Execute : BreakerTest
        {
            public Execute()
            {
                _breaker = new DummyBreaker();
            }

            [Fact]
            public void ReturnsTheResultWhenTheCircuitIsClosed()
            {
                Assert.True(_breaker.IsClosed);

                var result = _breaker.Execute();
                Assert.Equal("Dummy String", result);
            }

            [Fact]
            public void ReturnsTheResultWhenTheCircuitIsHalfOpen()
            {                
                _breaker.Try(_breaker.CurrentState);

                Assert.True(_breaker.IsHalfOpen);                
                Assert.Equal("Dummy String", _breaker.Execute());
            }

            [Fact]
            public void ThrowsWhenTheCircuitIsOpen()
            {
                _breaker.Trip(_breaker.CurrentState);
                Assert.True(_breaker.IsOpen);

                Assert.Throws<OpenCircuitBreakerException>(() => _breaker.Execute());
            }
        }

        public class Trip : BreakerTest
        {
            private static readonly CircuitBreakerOptions options = new CircuitBreakerOptions
            {
                ErrorThreshold = 2,
                SuccessThreshold = 1,
                InvocationTimeout = TimeSpan.FromMilliseconds(10),
                ResetTimeout = TimeSpan.FromMilliseconds(200)
            };

            public Trip()
            {
                _breaker = new DummyBreaker(TaskScheduler.Default, options);
                _breaker.Function = () => { throw new NotImplementedException(); };
            }

            [Fact]
            public void OccursWhenErrorThresholdIsReached()
            {
                Assert.Throws<NotImplementedException>(() => _breaker.Execute());
                Assert.Throws<NotImplementedException>(() => _breaker.Execute());                
                Assert.True(_breaker.IsOpen);
            }            

            [Fact]
            public void OccursWhenTimeoutsPassTheThreshold()
            {
                _breaker.Function = () => { Thread.Sleep(200); return "Some String"; };

                Assert.Throws<CircuitBreakerTimeoutException>(() => _breaker.Execute());
                Assert.Throws<CircuitBreakerTimeoutException>(() => _breaker.Execute());
                Assert.True(_breaker.IsOpen);
            }

            [Fact]
            public void ExceptionsAndTimeoutsCountTowardsThreshold()
            {
                Assert.Throws<NotImplementedException>(() => _breaker.Execute());
                _breaker.Function = () => { Thread.Sleep(200); return "Some String"; };

                Assert.Throws<CircuitBreakerTimeoutException>(() => _breaker.Execute());
                Assert.True(_breaker.IsOpen);
            }

            [Fact]
            public void OnlyTripsWhenTresholdReached()
            {
                Assert.Throws<NotImplementedException>(() => _breaker.Execute());                
                Assert.True(_breaker.IsClosed);
            }
        }

        public class Try : BreakerTest
        {
            private static readonly CircuitBreakerOptions options = new CircuitBreakerOptions
            {
                ErrorThreshold = 2,
                SuccessThreshold = 1,
                InvocationTimeout = TimeSpan.FromMilliseconds(50),
                ResetTimeout = TimeSpan.FromMilliseconds(20)
            };

            public Try()
            {
                _breaker = new DummyBreaker(TaskScheduler.Default, options);
            }

            [Fact]
            public void OccursAfterResetTimeoutWindowLapses()
            {
                _breaker.Function = () => { throw new NotImplementedException(); };

                Assert.Throws<NotImplementedException>(() => _breaker.Execute());
                Assert.Throws<NotImplementedException>(() => _breaker.Execute());
                Assert.True(_breaker.IsOpen);

                Thread.Sleep(50); // longer than reset timeout
                Assert.True(_breaker.IsHalfOpen);
            }
        }

        public class Reset : BreakerTest
        {
            private static readonly CircuitBreakerOptions options = new CircuitBreakerOptions
            {
                ErrorThreshold = 2,
                SuccessThreshold = 2,
                InvocationTimeout = TimeSpan.FromMilliseconds(50),
                ResetTimeout = TimeSpan.FromMilliseconds(20)
            };
            
            public Reset()
            {
                _breaker = new DummyBreaker(TaskScheduler.Default, options);
                _breaker.Try(_breaker.CurrentState);               
            }

            [Fact]
            public void OccursAfterSuccessThresholdIsReached()
            {
                Assert.True(_breaker.IsHalfOpen);

                _breaker.Execute();                
                _breaker.Execute();

                Assert.True(_breaker.IsClosed);
            }

            [Fact]
            public void OnlyOccursAfterThresholdIsReached()
            {
                Assert.True(_breaker.IsHalfOpen);

                _breaker.Execute();

                Assert.True(_breaker.IsHalfOpen);
            }
        }
    }
}
