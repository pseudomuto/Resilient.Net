using System;
using System.Threading.Tasks;
using NSubstitute;
using Xunit;

namespace Resilient.Net.Tests
{
    public static class OpenCircuitBreakerStateTest
    {
        public class Constructor
        {
            private readonly CircuitBreakerSwitch _switch = Substitute.For<CircuitBreakerSwitch>();
            private readonly CircuitBreakerInvoker _invoker = new CircuitBreakerInvoker(TaskScheduler.Default);

            [Fact]
            public void ThrowsWhenSwitchIsNull()
            {
                Assert.Throws<ArgumentNullException>(() => new OpenCircuitBreakerState(
                    null, 
                    _invoker, 
                    TimeSpan.FromMilliseconds(10)
                ));
            }

            [Fact]
            public void ThrowsWhenInvokerIsNull()
            {
                Assert.Throws<ArgumentNullException>(() => new OpenCircuitBreakerState(
                    _switch, 
                    null, 
                    TimeSpan.FromMilliseconds(10)
                ));
            }

            [Fact]
            public void ThrowsWhenTimeoutIsNotPositive()
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => new OpenCircuitBreakerState(
                    _switch, 
                    _invoker, 
                    TimeSpan.MinValue
                ));
            }
        }

        public class BecomeActive : CircuitBreakerStateTest<CircuitBreakerState>
        {
            public BecomeActive()
            {
                var invoker = new CircuitBreakerInvoker(TaskScheduler.Default);

                _state = new OpenCircuitBreakerState(
                    Substitute.For<CircuitBreakerSwitch>(), 
                    invoker, 
                    TimeSpan.FromMilliseconds(10)
                );
            }

            [Fact]
            public void SchedulesTransitionAfterTimeout()
            {
                var state = _state as OpenCircuitBreakerState;

                Assert.False(state.Scheduled);

                state.BecomeActive();
                Assert.True(state.Scheduled);
            }
        }

        public class Invoke
        {
            [Fact]
            public void ThrowsOpenCircuitBreakerException()
            {
                var state = new OpenCircuitBreakerState(
                    Substitute.For<CircuitBreakerSwitch>(), 
                    new CircuitBreakerInvoker(TaskScheduler.Default), 
                    TimeSpan.FromMilliseconds(1)
                );

                Assert.Throws<OpenCircuitBreakerException>(() => state.Invoke(() => ""));
            }
        }
    }
}
