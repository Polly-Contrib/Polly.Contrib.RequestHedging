using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Polly.Utilities;

namespace Polly.Contrib.RequestHedging
{
    internal static class AsyncHedgingEngine
    {
        internal static async Task<TResult> ImplementationAsync<TResult>(
            Func<Context, CancellationToken, Task<TResult>> action, Context context, CancellationToken cancellationToken,
            Func<Context, Task> onHedgeAsync, TimeSpan hedgingDelay,
            IEnumerable<Func<Context, CancellationToken, Task<TResult>>> hedgedTaskFunctions,
            bool continueOnCapturedContext)
        {
            IEnumerator<Func<Context, CancellationToken, Task<TResult>>> hedgedTaskFunctionsEnumerator = hedgedTaskFunctions?.GetEnumerator() ?? Enumerable.Empty<Func<Context, CancellationToken, Task<TResult>>>().GetEnumerator();

            var cancellationTokenList = new List<CancellationTokenSource>();
            var taskList = new List<Task<TResult>>();

            try
            {
                if (hedgedTaskFunctionsEnumerator.Current == null)
                {
                    return await action(context, cancellationToken).ConfigureAwait(continueOnCapturedContext);
                }

                taskList.Add(action(context, cancellationToken));

                while (taskList.Any(x => !x.IsCompleted))
                {
                    var delayTask = SystemClock.SleepAsync(hedgingDelay, cancellationToken);
                    var finishedTask = await Task.WhenAny(Task.WhenAny(taskList.Where(x => !x.IsCompleted)), delayTask).ConfigureAwait(continueOnCapturedContext);

                    if (finishedTask != delayTask)
                    {
                        // something completed before the delay, check and return the result if any
                        foreach (var t in taskList)
                        {
                            if (t.Status == TaskStatus.RanToCompletion)
                            {
                                return t.Result;
                            }
                        }
                    }

                    // no result returned, so maybe there is exception
                    // fire off hedge request if there is any
                    if (hedgedTaskFunctionsEnumerator.Current != null)
                    {
                        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        cancellationTokenList.Add(cts);
                        taskList.Add(hedgedTaskFunctionsEnumerator.Current(context, cts.Token));
                        await onHedgeAsync(context).ConfigureAwait(false);
                        hedgedTaskFunctionsEnumerator.MoveNext();
                    }
                }

                // all the task are completed, check if any ran to completion
                foreach (var t in taskList)
                {
                    if (t.Status == TaskStatus.RanToCompletion)
                    {
                        return t.Result;
                    }
                }

                // still no result, throw first exception
                foreach (var t in taskList)
                {
                    if (t.Status == TaskStatus.Faulted)
                    {
                        // this will rethrow the exception
                        await t.ConfigureAwait(continueOnCapturedContext);
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                // we should never reach here now
                throw new InvalidOperationException();
            }
            finally
            {
                // cancel all remaining tasks
                foreach (var taskCts in cancellationTokenList)
                {
                    taskCts.Cancel();
                    taskCts.Dispose();
                }

                // handle any faulted tasks
                foreach (var t in taskList.Where(x => x.IsFaulted))
                {
                    if (t.IsFaulted)
                    {
                        t.Exception.Handle(_ => true);
                    }
                }

                hedgedTaskFunctionsEnumerator?.Dispose();
            }
        }
    }
}