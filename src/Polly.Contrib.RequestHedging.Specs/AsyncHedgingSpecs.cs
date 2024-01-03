using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Polly.Contrib.RequestHedging.Specs
{
    public class AsyncHedgingSpecs
    {
        [Fact]
        public async Task Should_ExceptionPredicates_Invoked_But_Not_Match()
        {
            var hedgeCount = 0;

            await Assert.ThrowsAsync<TimeoutException>(() => Policy.Handle<InvalidOperationException>().HedgeAsync(
                    1, TimeSpan.FromMilliseconds(100), _ =>
                    {
                        Interlocked.Increment(ref hedgeCount);

                        return Task.CompletedTask;
                    })
                .ExecuteAsync(() => throw new TimeoutException()));

            Assert.Equal(0, hedgeCount);
        }

        [Fact]
        public async Task Should_ExceptionPredicates_Invoked_And_Matched()
        {
            var hedgeCount = 0;
            var invoked = false;

            await Policy.Handle<InvalidOperationException>().HedgeAsync(
                    1, TimeSpan.FromMilliseconds(100), _ =>
                    {
                        Interlocked.Increment(ref hedgeCount);

                        return Task.CompletedTask;
                    })
                .ExecuteAsync(() =>
                {
                    if (invoked) return Task.CompletedTask;

                    invoked = true;

                    throw new InvalidOperationException();
                });

            Assert.Equal(1, hedgeCount);
            Assert.True(invoked);
        }

        [Fact]
        public Task Always_Throw_Exception_And_Match() =>
            Assert.ThrowsAsync<TimeoutException>(() => Policy.Handle<TimeoutException>()
                .HedgeAsync(3, TimeSpan.FromMilliseconds(100))
                .ExecuteAsync(() => throw new TimeoutException()));

        [Fact]
        public async Task OnHedgeAsync_Count_Should_Equal_AttemptCount()
        {
            const int maxAttemptCount = 10;
            const int maxValue = 5;

            var hedgeCount = 0;
            var invokeCount = 0;

            await Policy.Handle<InvalidOperationException>()
                    .HedgeAsync(maxAttemptCount, TimeSpan.FromMilliseconds(20), _ =>
                    {
                        Interlocked.Increment(ref hedgeCount);

                        return Task.CompletedTask;
                    }).ExecuteAsync(() =>
                    {
                        if (invokeCount++ < maxValue) throw new InvalidOperationException();

                        return Task.CompletedTask;
                    });

            Assert.Equal(maxValue, hedgeCount);
        }

        [Fact]
        public async Task OnHedgeAsync_Count_Should_Equal_MaxAttemptCount()
        {
            const int maxAttemptCount = 10;

            var hedgeCount = 0;

            await Assert.ThrowsAsync<TimeoutException>(() => Policy.Handle<TimeoutException>()
                       .HedgeAsync(maxAttemptCount, TimeSpan.FromMilliseconds(20), _ =>
                       {
                           Interlocked.Increment(ref hedgeCount);

                           return Task.CompletedTask;
                       }).ExecuteAsync(() => throw new TimeoutException()));

            Assert.Equal(maxAttemptCount, hedgeCount);
        }
    }
}
