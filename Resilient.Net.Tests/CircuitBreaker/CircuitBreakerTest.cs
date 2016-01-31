using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Resilient.Net.Tests
{
    public static class CircuitBreakerTest
    {
        public abstract class BreakerTest : IDisposable
        {
            protected CircuitBreaker Breaker { get; set; }

            public Action Call<T>(Func<T> method)
            {
                return () => Breaker.Execute(method);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    Breaker.Dispose();
                }
            }
        }

        public class IsState : BreakerTest
        {
            public IsState()
            {
                Breaker = new CircuitBreaker();
            }

            [Fact]
            public void IsClosed()
            {
                Assert.True(Breaker.IsClosed);
                Assert.False(Breaker.IsOpen);
                Assert.False(Breaker.IsHalfOpen);
            }

            [Fact]
            public void IsOpen()
            {
                Breaker.Force(CircuitBreakerStateType.Open);

                Assert.True(Breaker.IsOpen);
                Assert.False(Breaker.IsClosed);
                Assert.False(Breaker.IsHalfOpen);
            }

            [Fact]
            public void IsHalfOpen()
            {
                Breaker.Force(CircuitBreakerStateType.HalfOpen);

                Assert.True(Breaker.IsHalfOpen);
                Assert.False(Breaker.IsClosed);
                Assert.False(Breaker.IsOpen);
            }
        }

        public class Logging : BreakerTest
        {
            private readonly TestTraceListener _traceLog = new TestTraceListener();

            public Logging()
            {
                Breaker = new CircuitBreaker();
                Trace.Listeners.Add(_traceLog);
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                Trace.Listeners.Remove(_traceLog);
            }

            [Fact]
            public void LogsRelevantInformationDuringATransition()
            {
                Breaker.Force(CircuitBreakerStateType.Open);

                var logLine = _traceLog.LastLine;

                Assert.Contains("[CircuitBreaker] state transition from Closed to Open", logLine);
                Assert.Contains("ErrorThreshold=2 SuccessThreshold=2", logLine);
                Assert.Contains("InvocationTimeout=1000 ResetTimeout=10000", logLine);
            }
        }

        public class Execute : BreakerTest
        {
            private readonly Func<string> _function = () => "Dummy String";

            public Execute()
            {
                Breaker = new CircuitBreaker();
            }

            [Fact]
            public void ReturnsTheResultWhenTheCircuitIsClosed()
            {
                Assert.True(Breaker.IsClosed);

                var result = Breaker.Execute(_function);
                Assert.Equal("Dummy String", result);
            }

            [Fact]
            public void ReturnsTheResultWhenTheCircuitIsHalfOpen()
            {
                Breaker.Force(CircuitBreakerStateType.HalfOpen);

                Assert.True(Breaker.IsHalfOpen);
                Assert.Equal("Dummy String", Breaker.Execute(_function));
            }

            [Fact]
            public void ThrowsWhenTheCircuitIsOpen()
            {
                Breaker.Force(CircuitBreakerStateType.Open);
                Assert.True(Breaker.IsOpen);

                Assert.Throws<OpenCircuitBreakerException>(Call(_function));
            }
        }

        public class Trip : BreakerTest
        {
            private static readonly int s_delay = 300;

            private static readonly CircuitBreakerOptions options = new CircuitBreakerOptions {
                ErrorThreshold = 2,
                SuccessThreshold = 1,
                InvocationTimeout = TimeSpan.FromMilliseconds(s_delay - 100),
                ResetTimeout = TimeSpan.FromMilliseconds(200)
            };

            public Trip()
            {
                Breaker = new CircuitBreaker(TaskScheduler.Default, options);
            }

            [Fact]
            public void OccursWhenErrorThresholdIsReached()
            {
                Assert.Throws<NotImplementedException>(Call(TestFunctions.NotImplemented));
                Assert.Throws<NotImplementedException>(Call(TestFunctions.NotImplemented));
                Assert.True(Breaker.IsOpen);
            }

            [Fact]
            public void OccursWhenTimeoutsPassTheThreshold()
            {
                Func<string> fn = TestFunctions.Delay(s_delay, () => "Some String");

                Assert.True(Breaker.IsClosed);

                Assert.Throws<CircuitBreakerTimeoutException>(Call(fn));
                Assert.Throws<CircuitBreakerTimeoutException>(Call(fn));
                Assert.True(Breaker.IsOpen);
            }

            [Fact]
            public void ExceptionsAndTimeoutsCountTowardsThreshold()
            {
                Assert.Throws<NotImplementedException>(Call(TestFunctions.NotImplemented));

                Func<string> fn = TestFunctions.Delay(s_delay, () => "Some String");

                Assert.Throws<CircuitBreakerTimeoutException>(Call(fn));
                Assert.True(Breaker.IsOpen);
            }

            [Fact]
            public void OnlyTripsWhenTresholdReached()
            {
                Assert.Throws<NotImplementedException>(Call(TestFunctions.NotImplemented));
                Assert.True(Breaker.IsClosed);
            }
        }

        public class Try : BreakerTest
        {
            private static readonly CircuitBreakerOptions options = new CircuitBreakerOptions {
                ErrorThreshold = 1,
                SuccessThreshold = 1,
                InvocationTimeout = TimeSpan.FromSeconds(1),
                ResetTimeout = TimeSpan.FromMilliseconds(20)
            };

            public Try()
            {
                Breaker = new CircuitBreaker(TaskScheduler.Default, options);
            }

            [Fact]
            public void OccursAfterResetTimeoutWindowLapses()
            {
                Assert.Throws<NotImplementedException>(Call(TestFunctions.NotImplemented));
                Assert.True(Breaker.IsOpen);

                Thread.Sleep(50); // longer than reset timeout
                Assert.True(Breaker.IsHalfOpen);
            }
        }

        public class Reset : BreakerTest
        {
            private static readonly CircuitBreakerOptions options = new CircuitBreakerOptions {
                ErrorThreshold = 2,
                SuccessThreshold = 2,
                InvocationTimeout = TimeSpan.FromMilliseconds(50),
                ResetTimeout = TimeSpan.FromMilliseconds(20)
            };

            public Reset()
            {
                Breaker = new CircuitBreaker(TaskScheduler.Default, options);
                Breaker.Force(CircuitBreakerStateType.HalfOpen);
            }

            [Fact]
            public void OccursAfterSuccessThresholdIsReached()
            {
                Assert.True(Breaker.IsHalfOpen);

                Breaker.Execute(() => "");
                Breaker.Execute(() => "");

                Assert.True(Breaker.IsClosed);
            }

            [Fact]
            public void OnlyOccursAfterThresholdIsReached()
            {
                Assert.True(Breaker.IsHalfOpen);

                Breaker.Execute(() => "");

                Assert.True(Breaker.IsHalfOpen);
            }
        }
    }
}
