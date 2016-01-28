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
            using (var token = new CancellationTokenSource(timeout))
            {
                try
                {
                    var task = Task<T>.Factory.StartNew(function, token.Token, TaskCreationOptions.None, _scheduler);

                    if (!TaskCompleted(task, timeout, token.Token))
                    {
                        throw new CircuitBreakerTimeoutException();
                    }

                    return task.Result;
                }
                catch (OperationCanceledException)
                {                    
                    throw new CircuitBreakerTimeoutException();
                }
            }
        }

        private static bool TaskCompleted<T>(Task<T> task, TimeSpan timeout, CancellationToken token)
        {
            try
            {
                if (task.IsCompleted)
                {
                    return true;
                }

                task.Wait(token);
                return true;
            }
            catch(AggregateException exc)
            {
                throw exc.GetBaseException();
            }
        }
    }
}
