using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resilient.Net.Test
{
    public class HalfOpenCircuitBreakerStateTest
    {
        [TestClass]
        public class Constructor
        {
            private CircuitBreakerSwitch _switch = Mock.Of<CircuitBreakerSwitch>();
            private CircuitBreakerInvoker _invoker = new CircuitBreakerInvoker(TaskScheduler.Default);

            [TestMethod]
            [ExpectedException(typeof(ArgumentNullException))]
            public void ThrowsWhenSwitchIsNull()
            {
                new HalfOpenCircuitBreakerState(null, _invoker, 1, TimeSpan.MaxValue);
            }

            [TestMethod]
            [ExpectedException(typeof(ArgumentNullException))]
            public void ThrowsWhenInvokerIsNull()
            {
                new HalfOpenCircuitBreakerState(_switch, null, 1, TimeSpan.MaxValue);
            }

            [TestMethod]
            [ExpectedException(typeof(ArgumentOutOfRangeException))]
            public void ThrowsWhenSuccessThresholdIsNotPositive()
            {
                new HalfOpenCircuitBreakerState(_switch, _invoker, 0, TimeSpan.MaxValue);
            }

            [TestMethod]
            [ExpectedException(typeof(ArgumentOutOfRangeException))]
            public void ThrowsWhenTimeoutIsNotPositive()
            {
                new HalfOpenCircuitBreakerState(_switch, _invoker, 1, TimeSpan.MinValue);
            }
        }

        [TestClass]
        public class Invoke
        {
            private TimeSpan _timeout = TimeSpan.FromMilliseconds(10);
            private Mock<CircuitBreakerInvoker> _mockInvoker = new Mock<CircuitBreakerInvoker>(TaskScheduler.Default);
            private HalfOpenCircuitBreakerState _state;

            [TestInitialize]
            public void Setup()
            {
                _state = new HalfOpenCircuitBreakerState(Mock.Of<CircuitBreakerSwitch>(), _mockInvoker.Object, 1, _timeout);
            }

            [TestMethod]
            public void InvokesTheFunction()
            {
                _state.Invoke(() => "some value");
                _mockInvoker.Verify(m => m.Invoke(_state, It.IsAny<Func<string>>(), _timeout));
            }

            [TestMethod]
            [ExpectedException(typeof(OpenCircuitBreakerException))]
            public void OnlyAttemptsInvocationsOneAtATime()
            {
                _state.Invoke(() => "");
                _state.Invoke(() => "");
            }
        }

        [TestClass]
        public class ExecutionSucceeded
        {
            private Mock<CircuitBreakerSwitch> _breakerSwitch = new Mock<CircuitBreakerSwitch>(MockBehavior.Strict);
            private CircuitBreakerInvoker _invoker = new CircuitBreakerInvoker(TaskScheduler.Default);
            private HalfOpenCircuitBreakerState _state;

            [TestInitialize]
            public void Setup()
            {
                _state = new HalfOpenCircuitBreakerState(_breakerSwitch.Object, _invoker, 2, TimeSpan.FromMilliseconds(10));
            }

            [TestMethod]
            public void ClosesTheBreakerWhenTheSuccessThresholdIsReached()
            {
                _breakerSwitch.Setup(m => m.Reset(_state));

                _state.ExecutionSucceeded();
                _state.ExecutionSucceeded();

                _breakerSwitch.VerifyAll();
            }

            [TestMethod]
            public void OnlyClosesTheBreakerWhenTheSuccessThresholdIsReached()
            {
                _state.ExecutionSucceeded();
                _breakerSwitch.VerifyAll();
            }
        }

        [TestClass]
        public class ExecutionFailed
        {
            private Mock<CircuitBreakerSwitch> _breakerSwitch = new Mock<CircuitBreakerSwitch>(MockBehavior.Strict);
            private CircuitBreakerInvoker _invoker = new CircuitBreakerInvoker(TaskScheduler.Default);
            private HalfOpenCircuitBreakerState _state;

            [TestInitialize]
            public void Setup()
            {
                _state = new HalfOpenCircuitBreakerState(_breakerSwitch.Object, _invoker, 2, TimeSpan.FromMilliseconds(10));
            }

            [TestMethod]
            public void TripsTheSwitch()
            {
                _breakerSwitch.Setup(m => m.Trip(_state));
                _state.ExecutionFailed();

                _breakerSwitch.VerifyAll();
            }
        }
    }
}
