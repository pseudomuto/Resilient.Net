using NSubstitute;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Resilient.Net.Tests
{
    public class ClosedCircuitBreakerStateTest
    {
        public class Constructor
        {
            private readonly CircuitBreakerSwitch _switch = Substitute.For<CircuitBreakerSwitch>();            
            private readonly CircuitBreakerInvoker _invoker = new CircuitBreakerInvoker(TaskScheduler.Default);

            [Fact]
            public void ThrowsWhenSwitchIsNull()
            {
                Assert.Throws<ArgumentNullException>(() => new ClosedCircuitBreakerState(null, _invoker, 1, TimeSpan.MaxValue));
            }

            [Fact]
            public void ThrowsWhenInvokerIsNull()
            {
                Assert.Throws<ArgumentNullException>(() => new ClosedCircuitBreakerState(_switch, null, 1, TimeSpan.MaxValue));
            }

            [Fact]
            public void ThrowsWhenErrorThresholdIsNotPositive()
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => new ClosedCircuitBreakerState(_switch, _invoker, 0, TimeSpan.MaxValue));
            }

            [Fact]
            public void ThrowsWhenTimeoutIsNotPositive()
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => new ClosedCircuitBreakerState(_switch, _invoker, 1, TimeSpan.MinValue));
            }
        }

        public class BecomeActive
        {
            private readonly CircuitBreakerInvoker _invoker = new CircuitBreakerInvoker(TaskScheduler.Default);
            private readonly ClosedCircuitBreakerState _state;
                        
            public BecomeActive()
            {
                _state = new ClosedCircuitBreakerState(Substitute.For<CircuitBreakerSwitch>(), _invoker, 2, TimeSpan.FromMilliseconds(10));
            }

            [Fact]
            public void ResetsFailureCount()
            {
                _state.ExecutionFailed();
                Assert.Equal(1, _state.Failures);

                _state.BecomeActive();
                Assert.Equal(0, _state.Failures);
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

                var state = new ClosedCircuitBreakerState(breakerSwitch, invoker, 1, timeout);
                state.Invoke(() => "whatever");

                invoker.Received().Invoke(state, Arg.Any<Func<string>>(), timeout);
            }
        }

        public class ExecutionSucceeded
        {            
            private readonly ClosedCircuitBreakerState _state;

            public ExecutionSucceeded()
            {
                var invoker = new CircuitBreakerInvoker(TaskScheduler.Default);
                _state = new ClosedCircuitBreakerState(Substitute.For<CircuitBreakerSwitch>(), invoker, 2, TimeSpan.FromMilliseconds(10));
            }

            [Fact]
            public void ResetsFailureCount()
            {
                _state.ExecutionFailed();
                Assert.Equal(1, _state.Failures);

                _state.ExecutionSucceeded();
                Assert.Equal(0, _state.Failures);
            }
        }

        public class ExecutionFailed
        {
            private CircuitBreakerSwitch _breakerSwitch = Substitute.For<CircuitBreakerSwitch>();
            private readonly CircuitBreakerInvoker _invoker = new CircuitBreakerInvoker(TaskScheduler.Default);
            private readonly ClosedCircuitBreakerState _state;

            public ExecutionFailed()
            {
                _state = new ClosedCircuitBreakerState(_breakerSwitch, _invoker, 2, TimeSpan.FromMilliseconds(10));
            }

            [Fact]
            public void TripsTheSwitchWhenThresholdReached()
            {
                _state.ExecutionFailed();
                _state.ExecutionFailed();

                _breakerSwitch.Received().Trip(_state);
            }

            [Fact]
            public void OnlyTripsTheSwitchWhenTheThresholdIsReached()
            {
                _state.ExecutionFailed();

                _breakerSwitch.DidNotReceive().Trip(_state);
            }
        }
    }
}
