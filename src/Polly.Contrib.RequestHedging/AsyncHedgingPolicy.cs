using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Polly.Contrib.RequestHedging
{
    /// <summary>
    /// A request hedging policy that can be applied to asynchronous delegates returning a value of type <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="TResult">.</typeparam>
    public class AsyncHedgingPolicy<TResult> : AsyncPolicy<TResult>, IHedgingPolicy<TResult>
    {
        private readonly Func<Context, Task> _onHedgeAsync;

        IEnumerable<Func<Context, CancellationToken, Task<TResult>>> _hedgedTaskFunctions;

        private readonly TimeSpan _hedgingDelay;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncHedgingPolicy{TResult}"/> class.
        /// </summary>
        /// <param name="policyBuilder">The policyBuilder<see cref="PolicyBuilder{TResult}"/>.</param>
        /// <param name="hedgedTaskFunctions">The hedgedTaskFunctions<see cref="IEnumerable{Func{Context, CancellationToken, Task{TResult}}}"/>.</param>
        /// <param name="hedgingDelay">The hedgingDelay<see cref="TimeSpan"/>.</param>
        /// <param name="onHedgeAsync">The onHedgeAsync<see cref="Func{Context, Task}"/>.</param>
        internal AsyncHedgingPolicy(
            PolicyBuilder<TResult> policyBuilder,
            IEnumerable<Func<Context, CancellationToken, Task<TResult>>> hedgedTaskFunctions,
            TimeSpan hedgingDelay,
            Func<Context, Task> onHedgeAsync)
            : base(policyBuilder)
        {
            _hedgedTaskFunctions = hedgedTaskFunctions ?? Enumerable.Empty<Func<Context, CancellationToken, Task<TResult>>>();
            _hedgingDelay = hedgingDelay;
            _onHedgeAsync = onHedgeAsync ?? (_ => Task.CompletedTask);
        }

        /// <inheritdoc/>
        [DebuggerStepThrough]
        protected override Task<TResult> ImplementationAsync(Func<Context, CancellationToken, Task<TResult>> action, Context context, CancellationToken cancellationToken,
            bool continueOnCapturedContext)
            => AsyncHedgingEngine.ImplementationAsync(
                action,
                context,
                cancellationToken,
                _onHedgeAsync,
                _hedgingDelay,
                _hedgedTaskFunctions,
                continueOnCapturedContext
            );
    }
}
