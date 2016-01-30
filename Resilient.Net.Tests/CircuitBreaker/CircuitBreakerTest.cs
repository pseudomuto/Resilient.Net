﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Resilient.Net.Tests
{
    public static class CircuitBreakerTest
    {
        private static readonly Func<string> NotImplemented = () =>
        {
            throw new NotImplementedException();
        };

        public abstract class BreakerTest : IDisposable
        {
            protected CircuitBreaker _breaker;

            public Action Call<T>(Func<T> method)
            {
                return () => _breaker.Execute(method);
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
                    _breaker.Dispose();
                }
            }
        }

        public class IsState : BreakerTest
        {
            public IsState()
            {
                _breaker = new CircuitBreaker();
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
                _breaker.Force(CircuitBreakerStateType.Open);

                Assert.True(_breaker.IsOpen);
                Assert.False(_breaker.IsClosed);
                Assert.False(_breaker.IsHalfOpen);
            }

            [Fact]
            public void IsHalfOpen()
            {
                _breaker.Force(CircuitBreakerStateType.HalfOpen);

                Assert.True(_breaker.IsHalfOpen);
                Assert.False(_breaker.IsClosed);
                Assert.False(_breaker.IsOpen);
            }
        }

        public class Execute : BreakerTest
        {
            private readonly Func<string> _function = () => "Dummy String";

            public Execute()
            {
                _breaker = new CircuitBreaker();
            }

            [Fact]
            public void ReturnsTheResultWhenTheCircuitIsClosed()
            {
                Assert.True(_breaker.IsClosed);

                var result = _breaker.Execute(_function);
                Assert.Equal("Dummy String", result);
            }

            [Fact]
            public void ReturnsTheResultWhenTheCircuitIsHalfOpen()
            {                
                _breaker.Force(CircuitBreakerStateType.HalfOpen);

                Assert.True(_breaker.IsHalfOpen);                
                Assert.Equal("Dummy String", _breaker.Execute(_function));
            }

            [Fact]
            public void ThrowsWhenTheCircuitIsOpen()
            {
                _breaker.Force(CircuitBreakerStateType.Open);
                Assert.True(_breaker.IsOpen);

                Assert.Throws<OpenCircuitBreakerException>(Call(_function));
            }
        }

        public class Trip : BreakerTest
        {
            private static readonly int delay = 300;

            private static readonly CircuitBreakerOptions options = new CircuitBreakerOptions {
                ErrorThreshold = 2,
                SuccessThreshold = 1,
                InvocationTimeout = TimeSpan.FromMilliseconds(delay - 100),
                ResetTimeout = TimeSpan.FromMilliseconds(200)
            };

            public Trip()
            {
                _breaker = new CircuitBreaker(TaskScheduler.Default, options);
            }

            [Fact]
            public void OccursWhenErrorThresholdIsReached()
            {
                Assert.Throws<NotImplementedException>(Call(NotImplemented));
                Assert.Throws<NotImplementedException>(Call(NotImplemented));
                Assert.True(_breaker.IsOpen);
            }

            [Fact]
            public void OccursWhenTimeoutsPassTheThreshold()
            {				
                Func<string> fn = () =>
                {
                    Thread.Sleep(delay);
                    return "Some String";
                };

                Assert.True(_breaker.IsClosed);

                Assert.Throws<CircuitBreakerTimeoutException>(Call(fn));
                Assert.Throws<CircuitBreakerTimeoutException>(Call(fn));
                Assert.True(_breaker.IsOpen);
            }

            [Fact]
            public void ExceptionsAndTimeoutsCountTowardsThreshold()
            {
                Assert.Throws<NotImplementedException>(Call(NotImplemented));

                Func<string> fn = () =>
                {
                    Thread.Sleep(delay);
                    return "Some String";
                };

                Assert.Throws<CircuitBreakerTimeoutException>(Call(fn));
                Assert.True(_breaker.IsOpen);
            }

            [Fact]
            public void OnlyTripsWhenTresholdReached()
            {
                Assert.Throws<NotImplementedException>(Call(NotImplemented));
                Assert.True(_breaker.IsClosed);
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
                _breaker = new CircuitBreaker(TaskScheduler.Default, options);
            }

            [Fact]
            public void OccursAfterResetTimeoutWindowLapses()
            {
                Assert.Throws<NotImplementedException>(Call(NotImplemented));
                Assert.True(_breaker.IsOpen);

                Thread.Sleep(50); // longer than reset timeout
                Assert.True(_breaker.IsHalfOpen);
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
                _breaker = new CircuitBreaker(TaskScheduler.Default, options);
                _breaker.Force(CircuitBreakerStateType.HalfOpen);               
            }

            [Fact]
            public void OccursAfterSuccessThresholdIsReached()
            {
                Assert.True(_breaker.IsHalfOpen);

                _breaker.Execute(() => "");                
                _breaker.Execute(() => "");

                Assert.True(_breaker.IsClosed);
            }

            [Fact]
            public void OnlyOccursAfterThresholdIsReached()
            {
                Assert.True(_breaker.IsHalfOpen);

                _breaker.Execute(() => "");

                Assert.True(_breaker.IsHalfOpen);
            }
        }
    }
}
