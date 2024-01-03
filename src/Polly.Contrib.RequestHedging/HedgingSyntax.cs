using System;
using System.Threading.Tasks;

namespace Polly.Contrib.RequestHedging
{
    /// <summary>
    ///     Fluent API for defining an <see cref="AsyncHedgingPolicy{TResult}" />.
    /// </summary>
    public static class HedgingSyntax
    {
        /// <summary>
        /// Builds an <see cref="AsyncHedgingPolicy{TResult}" /> that will send hedge request if the action does not succeed within
        /// <paramref name="hedgingDelay" /> and calls <paramref name="maxAttemptCount"/> times.
        /// </summary>
        /// <param name="policyBuilder">The policy builder.</param>
        /// <param name="maxAttemptCount">The maximum number of call attempts, not contains the first call.</param>
        /// <param name="hedgingDelay">Delay before issuing hedge request.</param>
        /// <param name="onHedgeAsync">Task performed after every spawning a hedged request.</param>
        /// <returns>The policy instance.</returns>
        /// <exception cref="ArgumentNullException">onRetry</exception>
        public static AsyncHedgingPolicy HedgeAsync(
            this PolicyBuilder policyBuilder,
            int maxAttemptCount,
            TimeSpan hedgingDelay,
            Func<Context, Task> onHedgeAsync = null)
            => new AsyncHedgingPolicy(
                policyBuilder,
                maxAttemptCount,
                hedgingDelay,
                onHedgeAsync);

        /// <summary>
        /// Builds an <see cref="AsyncHedgingPolicy{TResult}" /> that will send hedge request if the action does not succeed within
        /// <paramref name="hedgingDelay" /> and calls <paramref name="maxAttemptCount"/> times.
        /// </summary>
        /// <param name="policyBuilder">The policy builder.</param>
        /// <param name="maxAttemptCount">The maximum number of call attempts, not contains the first call.</param>
        /// <param name="hedgingDelay">Delay before issuing hedge request.</param>
        /// <param name="onHedgeAsync">Task performed after every spawning a hedged request.</param>
        /// <returns>The policy instance.</returns>
        /// <exception cref="ArgumentNullException">onRetry</exception>
        public static AsyncHedgingPolicy<TResult> HedgeAsync<TResult>(
            this PolicyBuilder<TResult> policyBuilder,
            int maxAttemptCount,
            TimeSpan hedgingDelay,
            Func<Context, Task> onHedgeAsync = null)
            => new AsyncHedgingPolicy<TResult>(
                policyBuilder,
                maxAttemptCount,
                hedgingDelay,
                onHedgeAsync);
    }
}
