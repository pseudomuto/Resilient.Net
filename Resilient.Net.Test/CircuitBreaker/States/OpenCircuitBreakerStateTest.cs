using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resilient.Net.Test
{
    public class OpenCircuitBreakerStateTest
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
                new OpenCircuitBreakerState(null, _invoker, TimeSpan.FromMilliseconds(10));
            }

            [TestMethod]
            [ExpectedException(typeof(ArgumentNullException))]
            public void ThrowsWhenInvokerIsNull()
            {
                new OpenCircuitBreakerState(_switch, null, TimeSpan.FromMilliseconds(10));
            }

            [TestMethod]
            [ExpectedException(typeof(ArgumentOutOfRangeException))]
            public void ThrowsWhenTimeoutIsNotPositive()
            {
                new OpenCircuitBreakerState(_switch, _invoker, TimeSpan.MinValue);
            }
        }

        [TestClass]
        public class BecomeActive
        {
            private Mock<CircuitBreakerSwitch> _breakerSwitch = new Mock<CircuitBreakerSwitch>(MockBehavior.Strict);
            private CircuitBreakerInvoker _invoker = new CircuitBreakerInvoker(TaskScheduler.Default);
            private OpenCircuitBreakerState _state;

            [TestInitialize]
            public void Setup()
            {
                _state = new OpenCircuitBreakerState(_breakerSwitch.Object, _invoker, TimeSpan.FromMilliseconds(10));
            }

            [TestMethod]
            public void SchedulesTransitionAfterTimeout()
            {
                Assert.IsFalse(_state.Scheduled);

                _state.BecomeActive();
                Assert.IsTrue(_state.Scheduled);
            }
        }

        [TestClass]
        public class Invoke
        {
            [TestMethod]
            [ExpectedException(typeof(OpenCircuitBreakerException))]
            public void ThrowsOpenCircuitBreakerException()
            {
                var state = new OpenCircuitBreakerState(Mock.Of<CircuitBreakerSwitch>(), new CircuitBreakerInvoker(TaskScheduler.Default), TimeSpan.FromMilliseconds(1));
                state.Invoke(() => "");
            }
        }
    }
}
