using System;
using System.Threading.Tasks;
using NSubstitute;
using Xunit;

namespace Resilient.Net.Tests
{
    public static class ClosedCircuitBreakerStateTest
    {
        public class Constructor
        {
            private readonly CircuitBreakerSwitch _switch = Substitute.For<CircuitBreakerSwitch>();

            private readonly CircuitBreakerInvoker _invoker = Substitute.For<CircuitBreakerInvoker>(
                                                                  TaskScheduler.Default
                                                              );

            [Fact]
            public void ThrowsWhenSwitchIsNull()
            {
                Assert.Throws<ArgumentNullException>(() => new ClosedCircuitBreakerState(
                    null, 
                    _invoker,
                    1, 
                    TimeSpan.MaxValue
                ));

            }

            [Fact]
            public void ThrowsWhenInvokerIsNull()
            {
                Assert.Throws<ArgumentNullException>(() => new ClosedCircuitBreakerState(
                    _switch, 
                    null, 
                    1, 
                    TimeSpan.MaxValue
                ));
            }

            [Fact]
            public void ThrowsWhenErrorThresholdIsNotPositive()
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => new ClosedCircuitBreakerState(
                    _switch, 
                    _invoker, 
                    0, 
                    TimeSpan.MaxValue
                ));
            }

            [Fact]
            public void ThrowsWhenTimeoutIsNotPositive()
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => new ClosedCircuitBreakerState(
                    _switch, 
                    _invoker, 
                    1, 
                    TimeSpan.MinValue
                ));
            }
        }

        public class Type : CircuitBreakerStateTest
        {
            public Type()
            {
                State = new ClosedCircuitBreakerState(
                    Substitute.For<CircuitBreakerSwitch>(),
                    Substitute.For<CircuitBreakerInvoker>(TaskScheduler.Default),
                    2,
                    TimeSpan.FromMilliseconds(10)
                );
            }

            [Fact]
            public void ReturnsClosed()
            {
                Assert.Equal(CircuitBreakerStateType.Closed, State.Type);
            }
        }

        public class BecomeActive : CircuitBreakerStateTest
        {
            public BecomeActive()
            {
                State = new ClosedCircuitBreakerState(
                    Substitute.For<CircuitBreakerSwitch>(), 
                    Substitute.For<CircuitBreakerInvoker>(TaskScheduler.Default), 
                    2, 
                    TimeSpan.FromMilliseconds(10)
                );
            }

            [Fact]
            public void ResetsFailureCount()
            {
                var state = As<ClosedCircuitBreakerState>();

                state.ExecutionFailed();
                Assert.Equal(1, state.Failures);

                state.BecomeActive();
                Assert.Equal(0, state.Failures);
            }
        }

        public class Invoke
        {
            [Fact]
            public void ProxiesCallToTheInvoker()
            {
                var breakerSwitch = Substitute.For<CircuitBreakerSwitch>();
                var invoker = Substitute.For<CircuitBreakerInvoker>(TaskScheduler.Default);
                var timeout = TimeSpan.FromMilliseconds(10);

                using (var state = new ClosedCircuitBreakerState(breakerSwitch, invoker, 1, timeout))
                {
                    state.Invoke(() => "whatever");

                    invoker.Received().Invoke(state, Arg.Any<Func<string>>(), timeout);
                }
            }
        }

        public class ExecutionSucceeded : CircuitBreakerStateTest
        {
            public ExecutionSucceeded()
            {                
                State = new ClosedCircuitBreakerState(
                    Substitute.For<CircuitBreakerSwitch>(), 
                    Substitute.For<CircuitBreakerInvoker>(TaskScheduler.Default), 
                    2, 
                    TimeSpan.FromMilliseconds(10)
                );
            }

            [Fact]
            public void ResetsFailureCount()
            {
                var state = As<ClosedCircuitBreakerState>();

                state.ExecutionFailed();
                Assert.Equal(1, state.Failures);

                state.ExecutionSucceeded();
                Assert.Equal(0, state.Failures);
            }
        }

        public class ExecutionFailed : CircuitBreakerStateTest
        {
            private readonly CircuitBreakerSwitch _breakerSwitch = Substitute.For<CircuitBreakerSwitch>();

            public ExecutionFailed()
            {
                State = new ClosedCircuitBreakerState(
                    _breakerSwitch, 
                    Substitute.For<CircuitBreakerInvoker>(TaskScheduler.Default), 
                    2, 
                    TimeSpan.FromMilliseconds(10)
                );
            }

            [Fact]
            public void TripsTheSwitchWhenThresholdReached()
            {
                State.ExecutionFailed();
                State.ExecutionFailed();

                _breakerSwitch.Received().Trip(State);
            }

            [Fact]
            public void OnlyTripsTheSwitchWhenTheThresholdIsReached()
            {
                State.ExecutionFailed();

                _breakerSwitch.DidNotReceive().Trip(State);
            }
        }
    }
}
