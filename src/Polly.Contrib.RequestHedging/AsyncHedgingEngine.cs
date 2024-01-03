using System;
using System.Threading;
using System.Threading.Tasks;

namespace Polly.Contrib.RequestHedging
{
    internal static class AsyncHedgingEngine
    {
        internal static async Task<TResult> ImplementationAsync<TResult>(
            Func<Context, CancellationToken, Task<TResult>> action, Context context,
            CancellationToken cancellationToken, ExceptionPredicates exceptionPredicates,
            ResultPredicates<TResult> resultPredicates, Func<Context, Task> onHedgeAsync, TimeSpan hedgingDelay,
            int maxAttemptCount, bool continueOnCapturedContext)
        {
            using (var result = new HedgingResult<TResult>(maxAttemptCount + 1, cancellationToken))
            {
                var now = DateTime.Now;

                result.Execute(action, context, exceptionPredicates, resultPredicates,
                    continueOnCapturedContext);

                for (var index = 0; index < maxAttemptCount; index++)
                {
                    var before = DateTime.Now - now;

                    var checkTask = true;
                    if (hedgingDelay > before)
                    {
                        var delayTask = Task.Delay(hedgingDelay - before, cancellationToken);

                        checkTask = await Task.WhenAny(result.Task, delayTask)
                            .ConfigureAwait(continueOnCapturedContext) != delayTask;
                    }

                    // something completed before the delay, check and return the result if any
                    if (checkTask && result.Task.IsCompleted)
                    {
                        break;
                    }

                    now = DateTime.Now;

                    // no result returned, so maybe there is result or exception match hedge request if there is any.
                    result.Execute(action, context, exceptionPredicates, resultPredicates,
                        continueOnCapturedContext);

                    await onHedgeAsync(context).ConfigureAwait(continueOnCapturedContext);
                }

                return result.Task.Status == TaskStatus.RanToCompletion
                    ? result.Task.Result
                    : await result.Task.ConfigureAwait(continueOnCapturedContext);
            }
        }

        private class HedgingResult<TResult> : IDisposable
        {
            private readonly TaskCompletionSource<TResult> _tcs = new TaskCompletionSource<TResult>();
            private readonly int _maxTasks;
            private readonly CancellationTokenSource _cts;

            private Exception _ex;
            private bool _hasResult;
            private TResult _result;
            private int _completedTasks;
            private bool _disposed;

            public Task<TResult> Task => _tcs.Task;

            public HedgingResult(int maxTasks, CancellationToken cancellationToken)
            {
                _maxTasks = maxTasks;

                _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                _cts.Token.Register(Cancel);
            }

            private void Cancel() => _tcs.TrySetCanceled(_cts.Token);

            public async void Execute(Func<Context, CancellationToken, Task<TResult>> action,
                Context context, ExceptionPredicates exceptionPredicates,
                ResultPredicates<TResult> resultPredicates, bool continueOnCapturedContext)
            {
                try
                {
                    var result = await action(context, _cts.Token).ConfigureAwait(continueOnCapturedContext);

                    TrySetResult(resultPredicates.AnyMatch(result), result);
                }
                catch (Exception ex)
                {
                    TrySetException(exceptionPredicates.FirstMatchOrDefault(ex) != null, ex);
                }
            }

            private void TrySetException(bool matched, Exception ex)
            {
                var completedTasks = Interlocked.Increment(ref _completedTasks);

                if (matched)
                {
                    _ex = ex;

                    TryCheckTasksHasCompletion(completedTasks);
                }
                else if (!_disposed && _cts.IsCancellationRequested)
                {
                    _tcs.TrySetCanceled(_cts.Token);
                }
                else if (ex is OperationCanceledException oe && oe.CancellationToken.IsCancellationRequested)
                {
                    _tcs.TrySetCanceled(oe.CancellationToken);
                }
                else
                {
                    _tcs.TrySetException(ex);
                }
            }

            private void TrySetResult(bool matched, TResult result)
            {
                var completedTasks = Interlocked.Increment(ref _completedTasks);

                if (matched)
                {
                    _hasResult = true;
                    _result = result;

                    TryCheckTasksHasCompletion(completedTasks);
                }
                else
                {
                    _tcs.TrySetResult(result);
                }
            }

            private void TryCheckTasksHasCompletion(int completedTasks)
            {
                if (completedTasks < _maxTasks) return;

                if (_hasResult)
                {
                    _tcs.TrySetResult(_result);
                }
                else if (!_disposed && _cts.IsCancellationRequested)
                {
                    _tcs.TrySetCanceled(_cts.Token);
                }
                else if (_ex is OperationCanceledException oe && oe.CancellationToken.IsCancellationRequested)
                {
                    _tcs.TrySetCanceled(oe.CancellationToken);
                }
                else
                {
                    _tcs.TrySetException(_ex ?? new InvalidOperationException());
                }
            }

            public void Dispose()
            {
                _cts.Cancel();

                _disposed = true;

                _cts.Dispose();
            }
        }
    }
}