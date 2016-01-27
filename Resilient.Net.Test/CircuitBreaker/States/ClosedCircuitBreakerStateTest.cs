using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resilient.Net.Test
{
    public class ClosedCircuitBreakerStateTest
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
                new ClosedCircuitBreakerState(null, _invoker, 1, TimeSpan.MaxValue);
            }

            [TestMethod]
            [ExpectedException(typeof(ArgumentNullException))]
            public void ThrowsWhenInvokerIsNull()
            {
                new ClosedCircuitBreakerState(_switch, null, 1, TimeSpan.MaxValue);
            }

            [TestMethod]
            [ExpectedException(typeof(ArgumentOutOfRangeException))]
            public void ThrowsWhenErrorThresholdIsNotPositive()
            {
                new ClosedCircuitBreakerState(_switch, _invoker, 0, TimeSpan.MaxValue);
            }

            [TestMethod]
            [ExpectedException(typeof(ArgumentOutOfRangeException))]
            public void ThrowsWhenTimeoutIsNotPositive()
            {
                new ClosedCircuitBreakerState(_switch, _invoker, 1, TimeSpan.MinValue);
            }
        }

        [TestClass]
        public class BecomeActive
        {
            private Mock<CircuitBreakerSwitch> _breakerSwitch = new Mock<CircuitBreakerSwitch>(MockBehavior.Strict);
            private CircuitBreakerInvoker _invoker = new CircuitBreakerInvoker(TaskScheduler.Default);
            private ClosedCircuitBreakerState _state;

            [TestInitialize]
            public void Setup()
            {
                _state = new ClosedCircuitBreakerState(_breakerSwitch.Object, _invoker, 2, TimeSpan.FromMilliseconds(10));
            }

            [TestMethod]
            public void ResetsFailureCount()
            {
                _state.ExecutionFailed();
                Assert.AreEqual(1, _state.Failures);

                _state.BecomeActive();
                Assert.AreEqual(0, _state.Failures);
            }
        }

        [TestClass]
        public class Invoke
        {
            private Mock<CircuitBreakerInvoker> _mockInvoker = new Mock<CircuitBreakerInvoker>(TaskScheduler.Default);

            [TestMethod]
            public void ProxiesCallToTheInvoker()
            {
                var breakerSwitch = Mock.Of<CircuitBreakerSwitch>();
                var invoker = new Mock<CircuitBreakerInvoker>(TaskScheduler.Default);
                var timeout = TimeSpan.FromMilliseconds(10);
                
                var state = new ClosedCircuitBreakerState(breakerSwitch, invoker.Object, 1, timeout);
                state.Invoke(() => "whatever");

                invoker.Verify(m => m.Invoke(state, It.IsAny<Func<string>>(), timeout));
            }
        }

        [TestClass]
        public class ExecutionSucceeded
        {
            private Mock<CircuitBreakerSwitch> _breakerSwitch = new Mock<CircuitBreakerSwitch>(MockBehavior.Strict);
            private CircuitBreakerInvoker _invoker = new CircuitBreakerInvoker(TaskScheduler.Default);
            private ClosedCircuitBreakerState _state;

            [TestInitialize]
            public void Setup()
            {
                _state = new ClosedCircuitBreakerState(_breakerSwitch.Object, _invoker, 2, TimeSpan.FromMilliseconds(10));
            }

            [TestMethod]
            public void ResetsFailureCount()
            {
                _state.ExecutionFailed();
                Assert.AreEqual(1, _state.Failures);

                _state.ExecutionSucceeded();
                Assert.AreEqual(0, _state.Failures);
            }
        }

        [TestClass]
        public class ExecutionFailed
        {
            private Mock<CircuitBreakerSwitch> _breakerSwitch = new Mock<CircuitBreakerSwitch>(MockBehavior.Strict);
            private CircuitBreakerInvoker _invoker = new CircuitBreakerInvoker(TaskScheduler.Default);
            private ClosedCircuitBreakerState _state;

            [TestInitialize]
            public void Setup()
            {
                _state = new ClosedCircuitBreakerState(_breakerSwitch.Object, _invoker, 2, TimeSpan.FromMilliseconds(10));
            }

            [TestMethod]
            public void TripsTheSwitchWhenThresholdReached()
            {
                _breakerSwitch.Setup(m => m.Trip(_state));

                _state.ExecutionFailed();
                _state.ExecutionFailed();

                _breakerSwitch.VerifyAll();
            }

            [TestMethod]
            public void OnlyTripsTheSwitchWhenTheThresholdIsReached()
            {                
                _state.ExecutionFailed();                
                _breakerSwitch.VerifyAll();
            }
        }
    }
}
