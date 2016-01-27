using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resilient.Net.Test
{
    public class CircuitBreakerTest
    {
        class DummyBreaker : CircuitBreaker<string>
        {
            public Func<string> Function { get; set; }

            public DummyBreaker()
            {
            }

            public DummyBreaker(TaskScheduler scheduler) 
                : base(scheduler)
            {
            }

            public DummyBreaker(TaskScheduler scheduler, CircuitBreakerOptions options) 
                : base(scheduler, options)
            {
            }
                        
            protected override string Perform()
            {
                if (Function != null)
                {
                    return Function.Invoke();
                }

                return "Dummy String";
            }
        }
        
        private static void AssertThrows<T>(Action action) where T : Exception
        {
            try
            {
                action.Invoke();
                Assert.Fail("{0} exception expected, but nothing was thrown", typeof(T).Name);
            }
            catch (T)
            {
                // this is good!
            }
            catch (Exception exc)
            {
                Assert.Fail("Expected {0} exception, but {1} was thrown instead", typeof(T).Name, exc.GetType().Name);
            }
        }

        [TestClass]
        public class IsState
        {
            private DummyBreaker _breaker = new DummyBreaker();

            [TestMethod]
            public void IsClosed()
            {
                Assert.IsTrue(_breaker.IsClosed);
                Assert.IsFalse(_breaker.IsOpen);
                Assert.IsFalse(_breaker.IsHalfOpen);
            }

            [TestMethod]
            public void IsOpen()
            {
                _breaker.Trip(_breaker.CurrentState);

                Assert.IsTrue(_breaker.IsOpen);
                Assert.IsFalse(_breaker.IsClosed);
                Assert.IsFalse(_breaker.IsHalfOpen);
            }

            [TestMethod]
            public void IsHalfOpen()
            {
                _breaker.Try(_breaker.CurrentState);

                Assert.IsTrue(_breaker.IsHalfOpen);
                Assert.IsFalse(_breaker.IsClosed);
                Assert.IsFalse(_breaker.IsOpen);
            }
        }

        [TestClass]
        public class Execute
        {
            private DummyBreaker _breaker = new DummyBreaker();

            [TestMethod]
            public void ReturnsTheResultWhenTheCircuitIsClosed()
            {
                Assert.IsTrue(_breaker.IsClosed);

                var result = _breaker.Execute();
                Assert.AreEqual("Dummy String", result);
            }

            [TestMethod]
            public void ReturnsTheResultWhenTheCircuitIsHalfOpen()
            {                
                _breaker.Try(_breaker.CurrentState);

                Assert.IsTrue(_breaker.IsHalfOpen);                
                Assert.AreEqual("Dummy String", _breaker.Execute());
            }

            [TestMethod]
            [ExpectedException(typeof(OpenCircuitBreakerException))]
            public void ThrowsWhenTheCircuitIsOpen()
            {
                _breaker.Trip(_breaker.CurrentState);
                Assert.IsTrue(_breaker.IsOpen);

                _breaker.Execute();
            }
        }

        [TestClass]
        public class Trip
        {
            private static readonly CircuitBreakerOptions options = new CircuitBreakerOptions
            {
                ErrorThreshold = 2,
                SuccessThreshold = 1,
                InvocationTimeout = TimeSpan.FromMilliseconds(10),
                ResetTimeout = TimeSpan.FromMilliseconds(200)
            };

            private readonly DummyBreaker _breaker = new DummyBreaker(TaskScheduler.Default, options);

            [TestInitialize]
            public void Setup()
            {
                _breaker.Function = () => { throw new NotImplementedException(); };
            }

            [TestMethod]
            public void OccursWhenErrorThresholdIsReached()
            {
                AssertThrows<NotImplementedException>(() => _breaker.Execute());
                AssertThrows<NotImplementedException>(() => _breaker.Execute());                
                Assert.IsTrue(_breaker.IsOpen);
            }            

            [TestMethod]
            public void OccursWhenTimeoutsPassTheThreshold()
            {
                _breaker.Function = () => { Thread.Sleep(200); return "Some String"; };

                AssertThrows<CircuitBreakerTimeoutException>(() => _breaker.Execute());
                AssertThrows<CircuitBreakerTimeoutException>(() => _breaker.Execute());
                Assert.IsTrue(_breaker.IsOpen);
            }

            [TestMethod]
            public void ExceptionsAndTimeoutsCountTowardsThreshold()
            {
                AssertThrows<NotImplementedException>(() => _breaker.Execute());
                _breaker.Function = () => { Thread.Sleep(200); return "Some String"; };

                AssertThrows<CircuitBreakerTimeoutException>(() => _breaker.Execute());
                Assert.IsTrue(_breaker.IsOpen);
            }

            [TestMethod]
            public void OnlyTripsWhenTresholdReached()
            {
                AssertThrows<NotImplementedException>(() => _breaker.Execute());                
                Assert.IsTrue(_breaker.IsClosed);
            }
        }

        [TestClass]
        public class Try
        {
            private static readonly CircuitBreakerOptions options = new CircuitBreakerOptions
            {
                ErrorThreshold = 2,
                SuccessThreshold = 1,
                InvocationTimeout = TimeSpan.FromMilliseconds(10),
                ResetTimeout = TimeSpan.FromMilliseconds(20)
            };

            private readonly DummyBreaker _breaker = new DummyBreaker(TaskScheduler.Default, options);

            [TestMethod]
            public void OccursAfterResetTimeoutWindowLapses()
            {
                _breaker.Function = () => { throw new NotImplementedException(); };

                AssertThrows<NotImplementedException>(() => _breaker.Execute());
                AssertThrows<NotImplementedException>(() => _breaker.Execute());
                Assert.IsTrue(_breaker.IsOpen);

                Thread.Sleep(50); // longer than reset timeout
                Assert.IsTrue(_breaker.IsHalfOpen);
            }
        }

        [TestClass]
        public class Reset
        {
            private static readonly CircuitBreakerOptions options = new CircuitBreakerOptions
            {
                ErrorThreshold = 2,
                SuccessThreshold = 2,
                InvocationTimeout = TimeSpan.FromMilliseconds(10),
                ResetTimeout = TimeSpan.FromMilliseconds(20)
            };

            private readonly DummyBreaker _breaker = new DummyBreaker(TaskScheduler.Default, options);

            [TestInitialize]
            public void Setup()
            {
                _breaker.Try(_breaker.CurrentState);               
            }

            [TestMethod]
            public void OccursAfterSuccessThresholdIsReached()
            {
                Assert.IsTrue(_breaker.IsHalfOpen);

                _breaker.Execute();                
                _breaker.Execute();

                Assert.IsTrue(_breaker.IsClosed);
            }

            [TestMethod]
            public void OnlyOccursAfterThresholdIsReached()
            {
                Assert.IsTrue(_breaker.IsHalfOpen);

                _breaker.Execute();

                Assert.IsTrue(_breaker.IsHalfOpen);
            }
        }
    }
}
