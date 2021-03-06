﻿using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Xunit;

namespace Resilient.Net.Tests
{
    public static class CircuitBreakerInvokerTest
    {
        public class Constructor
        {
            [Fact]
            public void WhenGivenAValidSchedule()
            {
                new CircuitBreakerInvoker(TaskScheduler.Current);
                Assert.True(true); // we made it!
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
            private readonly CircuitBreakerState _state = Substitute.For<CircuitBreakerState>();

            [Fact]
            public void WhenGivenANullFunctionThrows()
            {
                Assert.Throws<ArgumentNullException>(() => _invoker.Invoke<string>(
                    _state, 
                    null, 
                    TimeSpan.FromMilliseconds(10)
                ));
            }

            [Fact]
            public void WhenFunctionRunsSuccessfullyReturnsTheResult()
            {
                var expected = "Some value";
                var result = _invoker.Invoke(_state, () => expected, TimeSpan.FromSeconds(1));

                _state.Received().ExecutionSucceeded();
                Assert.Equal(expected, result);
            }

            [Fact]
            public void WhenTheTimeoutLapsesATimeoutExceptionIsThrown()
            {
                Func<string> fn = TestFunctions.Delay(50, () => "not used");

                Assert.Throws<CircuitBreakerTimeoutException>(() => _invoker.Invoke(
                    _state, 
                    fn, 
                    TimeSpan.FromMilliseconds(10)
                ));
            }

            [Fact]
            public void WhenATimeoutOccursTheStateIsNotified()
            {
                Func<string> fn = TestFunctions.Delay(50, () => "not used");

                Assert.Throws<CircuitBreakerTimeoutException>(() => 
					_invoker.Invoke(_state, fn, TimeSpan.FromMilliseconds(10))
                );

                _state.Received().ExecutionFailed();
            }

            [Fact]
            public void WhenTheFunctionThrowsItsExceptionIsPropagated()
            {
                var fn = TestFunctions.NotImplemented;

                Assert.Throws<NotImplementedException>(() => _invoker.Invoke(_state, fn, TimeSpan.FromSeconds(1)));
            }

            [Fact]
            public void WhenFunctionThrowsTheStateIsNotified()
            {
                var fn = TestFunctions.NotImplemented;

                Assert.Throws<NotImplementedException>(() => _invoker.Invoke(_state, fn, TimeSpan.FromSeconds(1)));
                _state.Received().ExecutionFailed();
            }
        }
    }
}
