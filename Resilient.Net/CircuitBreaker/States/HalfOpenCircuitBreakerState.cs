﻿using System;
using System.Threading;

namespace Resilient.Net
{
    internal class HalfOpenCircuitBreakerState : BaseCircuitBreakerState
    {
        private readonly int _successThreshold;
        private readonly TimeSpan _invocationTimeout;

        private int _successCount;
        private int _currentlyInvoking;

        public override CircuitBreakerStateType Type { get { return CircuitBreakerStateType.HalfOpen; } }

        public HalfOpenCircuitBreakerState(
            CircuitBreakerSwitch @switch, 
            CircuitBreakerInvoker invoker, 
            int successThreshold, 
            TimeSpan invocationTimeout)
            : base(@switch, invoker)
        {
            _successThreshold = successThreshold.PositiveOrThrow("successThreshold");
            _invocationTimeout = invocationTimeout.PositiveOrThrow("invocationTimeout");
        }

        public override T Invoke<T>(Func<T> function)
        {
            if (Interlocked.CompareExchange(ref _currentlyInvoking, 1, 0) == 0)
            {
                return Invoker.Invoke(this, function, _invocationTimeout);
            }

            throw new OpenCircuitBreakerException();
        }

        public override void ExecutionSucceeded()
        {
            Interlocked.CompareExchange(ref _currentlyInvoking, 0, 1);

            if (Interlocked.Increment(ref _successCount) == _successThreshold)
            {
                Switch.Reset(this);
            }
        }

        public override void ExecutionFailed()
        {
            Switch.Trip(this);
        }

        public override void BecomeActive()
        {
            Interlocked.Exchange(ref _successCount, 0);
            Interlocked.Exchange(ref _currentlyInvoking, 0);
        }
    }
}
