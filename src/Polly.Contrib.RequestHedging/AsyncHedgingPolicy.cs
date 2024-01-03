using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Polly.Contrib.RequestHedging
{
    /// <summary>
    /// A request hedging policy that can be applied to asynchronous delegates.
    /// </summary>
    public class AsyncHedgingPolicy : AsyncPolicy, IHedgingPolicy
    {
        private readonly Func<Context, Task> _onHedgeAsync;

        private readonly int _maxAttemptCount;
        private readonly TimeSpan _hedgingDelay;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncHedgingPolicy{TResult}"/> class.
        /// </summary>
        /// <param name="policyBuilder">The policyBuilder<see cref="PolicyBuilder{TResult}"/>.</param>
        /// <param name="maxAttemptCount">The maximum number of call attempts, not contains the first call.</param>
        /// <param name="hedgingDelay">The hedgingDelay<see cref="TimeSpan"/>.</param>
        /// <param name="onHedgeAsync">The onHedgeAsync<see cref="Func{Context, Task}"/>.</param>
        internal AsyncHedgingPolicy(
            PolicyBuilder policyBuilder,
            int maxAttemptCount,
            TimeSpan hedgingDelay,
            Func<Context, Task> onHedgeAsync)
            : base(policyBuilder)
        {
            _maxAttemptCount = maxAttemptCount;
            _hedgingDelay = hedgingDelay;
            _onHedgeAsync = onHedgeAsync ?? (_ => Task.CompletedTask);
        }

        /// <inheritdoc/>
        [DebuggerStepThrough]
        protected override Task<TResult> ImplementationAsync<TResult>(Func<Context, CancellationToken, Task<TResult>> action, Context context, CancellationToken cancellationToken,
            bool continueOnCapturedContext)
            => AsyncHedgingEngine.ImplementationAsync(
                action,
                context,
                cancellationToken,
                ExceptionPredicates,
                ResultPredicates<TResult>.None,
                _onHedgeAsync,
                _hedgingDelay,
                _maxAttemptCount,
                continueOnCapturedContext
            );
    }

    /// <summary>
    /// A request hedging policy that can be applied to asynchronous delegates returning a value of type <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="TResult">.</typeparam>
    public class AsyncHedgingPolicy<TResult> : AsyncPolicy<TResult>, IHedgingPolicy<TResult>
    {
        private readonly Func<Context, Task> _onHedgeAsync;

        private readonly int _maxAttemptCount;
        private readonly TimeSpan _hedgingDelay;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncHedgingPolicy{TResult}"/> class.
        /// </summary>
        /// <param name="policyBuilder">The policyBuilder<see cref="PolicyBuilder{TResult}"/>.</param>
        /// <param name="maxAttemptCount">The maximum number of call attempts, not contains the first call.</param>
        /// <param name="hedgingDelay">The hedgingDelay<see cref="TimeSpan"/>.</param>
        /// <param name="onHedgeAsync">The onHedgeAsync<see cref="Func{Context, Task}"/>.</param>
        internal AsyncHedgingPolicy(
            PolicyBuilder<TResult> policyBuilder,
            int maxAttemptCount,
            TimeSpan hedgingDelay,
            Func<Context, Task> onHedgeAsync)
            : base(policyBuilder)
        {
            _maxAttemptCount = maxAttemptCount;
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
                ExceptionPredicates,
                ResultPredicates,
                _onHedgeAsync,
                _hedgingDelay,
                _maxAttemptCount,
                continueOnCapturedContext
            );
    }
}
