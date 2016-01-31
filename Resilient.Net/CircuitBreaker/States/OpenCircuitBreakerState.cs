﻿using System;
using System.Threading;

namespace Resilient.Net
{
    internal class OpenCircuitBreakerState : BaseCircuitBreakerState
    {
        private Timer _timer;
        private readonly TimeSpan _resetTimeout;

        public override CircuitBreakerStateType Type { get { return CircuitBreakerStateType.Open; } }

        public bool Scheduled { get; private set; }

        public OpenCircuitBreakerState(CircuitBreakerSwitch breakerSwitch, CircuitBreakerInvoker invoker, TimeSpan resetTimeout)
            : base(breakerSwitch, invoker)
        {
            _resetTimeout = resetTimeout.PositiveOrThrow("resetTimeout");
        }

        public override T Invoke<T>(Func<T> function)
        {
            throw new OpenCircuitBreakerException();
        }

        public override void ExecutionSucceeded()
        {
        }

        public override void ExecutionFailed()
        {
        }

        public override void BecomeActive()
        {
            if (_timer != null)
            {
                _timer.Dispose();
            }

            _timer = new Timer(_ => HalfOpen(), null, (int)_resetTimeout.TotalMilliseconds, Timeout.Infinite);
            Scheduled = true;
        }

        private void HalfOpen()
        {
            Switch.Try(this);
            Scheduled = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _timer != null)
            {
                _timer.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
