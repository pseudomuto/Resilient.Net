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
            private readonly CircuitBreakerInvoker _invoker = new CircuitBreakerInvoker(TaskScheduler.Default);

            [Fact]
            public void ThrowsWhenSwitchIsNull()
            {
                Assert.Throws<ArgumentNullException>(() => new HalfOpenCircuitBreakerState(null, _invoker, 1, TimeSpan.MaxValue));
            }

            [Fact]
            public void ThrowsWhenInvokerIsNull()
            {
                Assert.Throws<ArgumentNullException>(() => new HalfOpenCircuitBreakerState(_switch, null, 1, TimeSpan.MaxValue));
            }

            [Fact]
            public void ThrowsWhenSuccessThresholdIsNotPositive()
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => new HalfOpenCircuitBreakerState(_switch, _invoker, 0, TimeSpan.MaxValue));
            }

            [Fact]
            public void ThrowsWhenTimeoutIsNotPositive()
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => new HalfOpenCircuitBreakerState(_switch, _invoker, 1, TimeSpan.MinValue));
            }
        }

        public class Type : CircuitBreakerStateTest<CircuitBreakerState>
        {
            public Type()
            {
                _state = new HalfOpenCircuitBreakerState(
                    Substitute.For<CircuitBreakerSwitch>(),
                    Substitute.For<CircuitBreakerInvoker>(TaskScheduler.Default),
                    2,
                    TimeSpan.FromMilliseconds(10)
                );
            }

            [Fact]
            public void ReturnsHalfOpen()
            {
                Assert.Equal(CircuitBreakerStateType.HalfOpen, (_state as HalfOpenCircuitBreakerState).Type);
            }
        }

        public class Invoke : CircuitBreakerStateTest<CircuitBreakerState>
        {
            private readonly TimeSpan _timeout = TimeSpan.FromMilliseconds(10);
            private readonly CircuitBreakerInvoker _mockInvoker = Substitute.For<CircuitBreakerInvoker>(TaskScheduler.Default);

            public Invoke()
            {
                _state = new HalfOpenCircuitBreakerState(Substitute.For<CircuitBreakerSwitch>(), _mockInvoker, 1, _timeout);
            }

            [Fact]
            public void InvokesTheFunction()
            {
                _state.Invoke(() => "some value");
                _mockInvoker.Received().Invoke(_state, Arg.Any<Func<string>>(), _timeout);
            }

            [Fact]
            public void OnlyAttemptsInvocationsOneAtATime()
            {
                _state.Invoke(() => "");
                Assert.Throws<OpenCircuitBreakerException>(() => _state.Invoke(() => ""));
            }
        }

        public class ExecutionSucceeded : CircuitBreakerStateTest<CircuitBreakerState>
        {
            private readonly CircuitBreakerSwitch _breakerSwitch = Substitute.For<CircuitBreakerSwitch>();
            private readonly CircuitBreakerInvoker _invoker = new CircuitBreakerInvoker(TaskScheduler.Default);

            public ExecutionSucceeded()
            {
                _state = new HalfOpenCircuitBreakerState(_breakerSwitch, _invoker, 2, TimeSpan.FromMilliseconds(10));
            }

            [Fact]
            public void ClosesTheBreakerWhenTheSuccessThresholdIsReached()
            {
                _state.ExecutionSucceeded();
                _state.ExecutionSucceeded();

                _breakerSwitch.Received().Reset(_state);
            }

            [Fact]
            public void OnlyClosesTheBreakerWhenTheSuccessThresholdIsReached()
            {
                _state.ExecutionSucceeded();

                _breakerSwitch.DidNotReceive().Reset(_state);
            }
        }

        public class ExecutionFailed : CircuitBreakerStateTest<CircuitBreakerState>
        {
            private readonly CircuitBreakerSwitch _breakerSwitch = Substitute.For<CircuitBreakerSwitch>();
            private readonly CircuitBreakerInvoker _invoker = new CircuitBreakerInvoker(TaskScheduler.Default);

            public ExecutionFailed()
            {
                _state = new HalfOpenCircuitBreakerState(_breakerSwitch, _invoker, 2, TimeSpan.FromMilliseconds(10));
            }

            [Fact]
            public void TripsTheSwitch()
            {
                _state.ExecutionFailed();

                _breakerSwitch.Received().Trip(_state);
            }
        }
    }
}
