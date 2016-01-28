using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resilient.Net
{
    internal class CircuitBreakerInvoker
    {
        private TaskScheduler _scheduler;

        public CircuitBreakerInvoker(TaskScheduler scheduler)
        {
            _scheduler = scheduler.ValueOrThrow("scheduler");
        }

        public virtual T Invoke<T>(CircuitBreakerState state, Func<T> function, TimeSpan timeout)
        {
            T result = default(T);

            try
            {
                result = Invoke(function, timeout);
                state.ExecutionSucceeded();
            }
            catch(Exception)
            {
                state.ExecutionFailed();
                throw;
            }

            return result;
        }

        private T Invoke<T>(Func<T> function, TimeSpan timeout)
        {
            function.EnsureNotNull("function");

            using (var token = new CancellationTokenSource())
            {
                var task = Task<T>.Factory.StartNew(function, token.Token, TaskCreationOptions.None, _scheduler);

                if (!TaskCompleted(task, timeout, token))
                {
                    token.Cancel(true);
                    throw new CircuitBreakerTimeoutException();                    
                }

                return task.Result;
            }
        }

        private static bool TaskCompleted<T>(Task<T> task, TimeSpan timeout, CancellationTokenSource token)
        {
            try
            {
                return task.IsCompleted || task.Wait((int)timeout.TotalMilliseconds, token.Token);
            }
            catch(AggregateException exc)
            {            
                throw exc.GetBaseException();
            }
        }
    }
}
