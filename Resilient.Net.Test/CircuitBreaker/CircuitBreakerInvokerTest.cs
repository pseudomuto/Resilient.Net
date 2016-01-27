using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resilient.Net.Test
{
    public class CircuitBreakerInvokerTest
    {
        [TestClass]
        public class Constructor
        {
            [TestMethod]
            public void WhenGivenAValidSchedule()
            {
                new CircuitBreakerInvoker(TaskScheduler.Current);
            }

            [TestMethod]
            [ExpectedException(typeof(ArgumentNullException))]
            public void WhenGivenANullTaskScheduler()
            {
                new CircuitBreakerInvoker(null);
            }
        }

        [TestClass]
        public class Invoke
        {
            private CircuitBreakerInvoker _invoker = new CircuitBreakerInvoker(TaskScheduler.Default);
            private Mock<CircuitBreakerState> _mock = new Mock<CircuitBreakerState>();
            
            [TestMethod]
            public void WhenSuccessfulReturnsTheResultAndNotifiesState()
            {
                var expected = "Some value";
                var result = _invoker.Invoke(_mock.Object, () => expected, TimeSpan.FromSeconds(1));

                _mock.Verify(m => m.ExecutionSucceeded());
                Assert.AreEqual(expected, result);
            }

            [TestMethod]
            public void WhenFailedReturnsTheDefaultResultAndNotifiesState()
            {
                Func<string> cmd = () => { throw new Exception("Boom-Shakalaka"); };

                try
                {
                    _invoker.Invoke(_mock.Object, cmd, TimeSpan.FromSeconds(1));
                    Assert.Fail("Shouldn't have gotten here");
                }
                catch (Exception exception)
                {
                    Assert.AreEqual("Boom-Shakalaka", exception.Message);
                    _mock.Verify(m => m.ExecutionFailed());
                }
            }
        }
    }
}
