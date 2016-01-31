using System;
using System.Threading.Tasks;
using NSubstitute;
using Xunit;

namespace Resilient.Net.Tests
{
    public static class HalfOpenCircuitBreakerStateTest
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
                Assert.Throws<ArgumentNullException>(() => new HalfOpenCircuitBreakerState(
                    null, 
                    _invoker, 
                    1, 
                    TimeSpan.MaxValue
                ));
            }

            [Fact]
            public void ThrowsWhenInvokerIsNull()
            {
                Assert.Throws<ArgumentNullException>(() => new HalfOpenCircuitBreakerState(
                    _switch,
                    null,
                    1, 
                    TimeSpan.MaxValue
                ));
            }

            [Fact]
            public void ThrowsWhenSuccessThresholdIsNotPositive()
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => new HalfOpenCircuitBreakerState(
                    _switch, 
                    _invoker,
                    0, 
                    TimeSpan.MaxValue
                ));
            }

            [Fact]
            public void ThrowsWhenTimeoutIsNotPositive()
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => new HalfOpenCircuitBreakerState(
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
                State = new HalfOpenCircuitBreakerState(
                    Substitute.For<CircuitBreakerSwitch>(),
                    Substitute.For<CircuitBreakerInvoker>(TaskScheduler.Default),
                    2,
                    TimeSpan.FromMilliseconds(10)
                );
            }

            [Fact]
            public void ReturnsHalfOpen()
            {
                Assert.Equal(CircuitBreakerStateType.HalfOpen, State.Type);
            }
        }

        public class Invoke : CircuitBreakerStateTest
        {
            private readonly TimeSpan _timeout = TimeSpan.FromMilliseconds(10);

            private readonly CircuitBreakerInvoker _invoker = Substitute.For<CircuitBreakerInvoker>(
                                                                  TaskScheduler.Default
                                                              );

            public Invoke()
            {
                State = new HalfOpenCircuitBreakerState(
                    Substitute.For<CircuitBreakerSwitch>(), 
                    _invoker,
                    1, 
                    _timeout
                );
            }

            [Fact]
            public void InvokesTheFunction()
            {
                State.Invoke(() => "some value");
                _invoker.Received().Invoke(State, Arg.Any<Func<string>>(), _timeout);
            }

            [Fact]
            public void OnlyAttemptsInvocationsOneAtATime()
            {
                State.Invoke(() => "");
                Assert.Throws<OpenCircuitBreakerException>(() => State.Invoke(() => ""));
            }
        }

        public class ExecutionSucceeded : CircuitBreakerStateTest
        {
            private readonly CircuitBreakerSwitch _breakerSwitch = Substitute.For<CircuitBreakerSwitch>();

            public ExecutionSucceeded()
            {
                State = new HalfOpenCircuitBreakerState(
                    _breakerSwitch, 
                    Substitute.For<CircuitBreakerInvoker>(TaskScheduler.Default), 
                    2, 
                    TimeSpan.FromMilliseconds(10)
                );
            }

            [Fact]
            public void ClosesTheBreakerWhenTheSuccessThresholdIsReached()
            {
                State.ExecutionSucceeded();
                State.ExecutionSucceeded();

                _breakerSwitch.Received().Reset(State);
            }

            [Fact]
            public void OnlyClosesTheBreakerWhenTheSuccessThresholdIsReached()
            {
                State.ExecutionSucceeded();

                _breakerSwitch.DidNotReceive().Reset(State);
            }
        }

        public class ExecutionFailed : CircuitBreakerStateTest
        {
            private readonly CircuitBreakerSwitch _breakerSwitch = Substitute.For<CircuitBreakerSwitch>();

            public ExecutionFailed()
            {
                State = new HalfOpenCircuitBreakerState(
                    _breakerSwitch, 
                    new CircuitBreakerInvoker(TaskScheduler.Default), 
                    2, 
                    TimeSpan.FromMilliseconds(10)
                );
            }

            [Fact]
            public void TripsTheSwitch()
            {
                State.ExecutionFailed();

                _breakerSwitch.Received().Trip(State);
            }
        }
    }
}
