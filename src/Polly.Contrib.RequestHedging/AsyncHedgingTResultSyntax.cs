using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Polly.Contrib.RequestHedging
{
    /// <summary>
    ///     Fluent API for defining an <see cref="AsyncHedgingPolicy{TResult}" />.
    /// </summary>
    public static class AsyncHedgingTResultSyntax
    {
        /// <summary>
        /// Builds an <see cref="AsyncHedgingPolicy{TResult}" /> that will send hedge request if the action does not succeed within
        /// <paramref name="hedgingDelay" />. It will iterate through each of the <paramref name="hedgedTaskFunctions"/>
        /// </summary>
        /// <param name="policyBuilder">The policy builder.</param>
        /// <param name="hedgedTaskFunctions">Hedge request function list.</param>
        /// <param name="hedgingDelay">Delay before issuing hedge request.</param>
        /// <param name="onHedgeAsync">Task performed after every spawning of a hedged request.</param>
        /// <returns>The policy instance.</returns>
        /// <exception cref="ArgumentNullException">onRetry</exception>
        public static AsyncHedgingPolicy<TResult> HedgeAsync<TResult>(
            this PolicyBuilder<TResult> policyBuilder,
            IEnumerable<Func<Context, CancellationToken, Task<TResult>>> hedgedTaskFunctions,
            TimeSpan hedgingDelay,
            Func<Context, Task> onHedgeAsync = null)
        {
            if (hedgingDelay == default || hedgingDelay == System.Threading.Timeout.InfiniteTimeSpan) throw new ArgumentException(nameof(hedgingDelay));

            return new AsyncHedgingPolicy<TResult>(
                policyBuilder,
                hedgedTaskFunctions,
                hedgingDelay,
                onHedgeAsync);
        }

        /// <summary>
        /// Builds an <see cref="AsyncHedgingPolicy{TResult}" /> that will send hedge request if the action does not succeed within
        /// <paramref name="hedgingDelay" /> and perform <paramref name="hedgedTaskFunction"/> via hedging.
        /// </summary>        
        /// <param name="policyBuilder">The policy builder.</param>
        /// <param name="hedgedTaskFunction">Hedge request function.</param>
        /// <param name="hedgingDelay">Delay before issuing hedge request.</param>
        /// <param name="onHedgeAsync">Task performed after every spawning of a hedged request</param>
        /// <returns>The policy instance.</returns>
        /// <exception cref="ArgumentNullException">onRetry</exception>
        public static AsyncHedgingPolicy<TResult> HedgeAsync<TResult>(
            this PolicyBuilder<TResult> policyBuilder,
            Func<Context, CancellationToken, Task<TResult>> hedgedTaskFunction,
            TimeSpan hedgingDelay,
            Func<Context, Task> onHedgeAsync = null)
            => HedgeAsync<TResult>(policyBuilder, new[] { hedgedTaskFunction }, hedgingDelay, onHedgeAsync);

        /// <summary>
        /// Builds an <see cref="AsyncHedgingPolicy{TResult}" /> that will send hedge request if the action does not succeed within
        /// <paramref name="hedgingDelay" /> and calls <paramref name="hedgedTaskFunction"/> <paramref name="hedgeCallAttempts"/> times.
        /// </summary>        
        /// <param name="policyBuilder">The policy builder.</param>
        /// <param name="hedgedTaskFunction">Hedge request function.</param>
        /// <param name="hedgeCallAttempts">Number of times to call the hedge function.</param>
        /// <param name="hedgingDelay">Delay before issuing hedge request.</param>
        /// <param name="onHedgeAsync">Task performed after every spawning a hedged request.</param>
        /// <returns>The policy instance.</returns>
        /// <exception cref="ArgumentNullException">onRetry</exception>
        public static AsyncHedgingPolicy<TResult> HedgeAsync<TResult>(
            this PolicyBuilder<TResult> policyBuilder,
            Func<Context, CancellationToken, Task<TResult>> hedgedTaskFunction,
            int hedgeCallAttempts,
            TimeSpan hedgingDelay,
            Func<Context, Task> onHedgeAsync = null)
            => HedgeAsync<TResult>(policyBuilder, Enumerable.Repeat(hedgedTaskFunction, hedgeCallAttempts), hedgingDelay, onHedgeAsync);
    }
}
