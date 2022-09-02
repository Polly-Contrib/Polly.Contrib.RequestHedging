using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Polly.Contrib.RequestHedging.Specs
{
    public class AsyncHedgingTResultSpecs
    {
        [Fact]
        public async Task Should_ExceptionPredicates_Invoked_But_Not_Match()
        {
            var hedgeCount = 0;

            await Assert.ThrowsAsync<TimeoutException>(() => Policy<int>.Handle<InvalidOperationException>().HedgeAsync(
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

            await Policy<int>.Handle<InvalidOperationException>().HedgeAsync(
                    1, TimeSpan.FromMilliseconds(100), _ =>
                    {
                        Interlocked.Increment(ref hedgeCount);

                        return Task.CompletedTask;
                    })
                .ExecuteAsync(() =>
                {
                    if (invoked) return Task.FromResult(1);

                    invoked = true;

                    throw new InvalidOperationException();
                });

            Assert.Equal(1, hedgeCount);
            Assert.True(invoked);
        }

        [Fact]
        public Task Always_Throw_Exception_And_Match() =>
            Assert.ThrowsAsync<TimeoutException>(() => Policy<bool>.Handle<TimeoutException>()
                .HedgeAsync(3, TimeSpan.FromMilliseconds(100))
                .ExecuteAsync(() => throw new TimeoutException()));

        [Fact]
        public async Task Should_ResultPredicates_Invoked()
        {
            var invokeCount = 0;

            await Policy<int>.HandleResult(x => x < 1).HedgeAsync(3, TimeSpan.FromMilliseconds(100))
                .ExecuteAsync(() => Task.FromResult(invokeCount++));

            Assert.Equal(2, invokeCount);
        }

        [Fact]
        public async Task OnHedgeAsync_Count_Should_Equal_AttemptCount()
        {
            const int maxAttemptCount = 10;
            const int maxValue = 5;

            var hedgeCount = 0;
            var invokeCount = 0;

            await Policy<int>.HandleResult(x => x < maxValue)
                    .HedgeAsync(maxAttemptCount, TimeSpan.FromMilliseconds(20), _ =>
                    {
                        Interlocked.Increment(ref hedgeCount);

                        return Task.CompletedTask;
                    }).ExecuteAsync(() => Task.FromResult(invokeCount++));

            Assert.Equal(maxValue, hedgeCount);
        }

        [Fact]
        public async Task OnHedgeAsync_Count_Should_Equal_MaxAttemptCount()
        {
            const int maxAttemptCount = 10;

            var hedgeCount = 0;

            await Policy<int>.HandleResult(x => x < 10)
                        .HedgeAsync(maxAttemptCount, TimeSpan.FromMilliseconds(20), _ =>
                        {
                            Interlocked.Increment(ref hedgeCount);

                            return Task.CompletedTask;
                        }).ExecuteAsync(() => Task.FromResult(0));

            Assert.Equal(maxAttemptCount, hedgeCount);
        }

        [Fact]
        public async Task Should_Prefer_Result_InsteadOf_Exception()
        {
            var invoked = false;

            Assert.False(await Policy<bool>.HandleResult(x => !x).Or<InvalidOperationException>()
                .HedgeAsync(1, TimeSpan.FromMilliseconds(100))
                .ExecuteAsync(() =>
                {
                    if (invoked) throw new InvalidOperationException();

                    invoked = true;

                    return Task.FromResult(false);
                }));
        }

        [Fact]
        public async Task Should_Return_The_Fastest_Result()
        {
            var array = new[] { (500, 3), (250, 15), (200, 20) };
            var count = 0;

            Assert.Equal(15, await Policy<int>.HandleResult(x => x < 10).HedgeAsync(array.Length - 1, TimeSpan.FromMilliseconds(100))
                .ExecuteAsync(async () =>
                {
                    var item = array[count++];

                    await Task.Delay(item.Item1);

                    return item.Item2;
                }));
        }

        [Fact]
        public async Task Should_Return_The_Fastest_Result_Slowly()
        {
            var array = new[] { (500, 3), (350, 15), (200, 20) };
            var count = 0;

            Assert.Equal(20, await Policy<int>.HandleResult(x => x < 10).HedgeAsync(array.Length - 1, TimeSpan.FromMilliseconds(100))
                .ExecuteAsync(async () =>
                {
                    var item = array[count++];

                    await Task.Delay(item.Item1);

                    return item.Item2;
                }));
        }

        [Fact]
        public async Task Completed_Before_Any_Hedge_Request()
        {
            var invokeCount = 0;

            Assert.Equal(0, await Policy<int>.Handle<Exception>().HedgeAsync(10, TimeSpan.FromMilliseconds(1000))
               .ExecuteAsync(async () =>
               {
                   await Task.Delay(50);

                   return invokeCount++;
               }));
        }

        [Fact]
        public async Task Return_Least_Matched_Result()
        {
            var invokeCount = 0;

            Assert.Equal(3, await Policy<int>.HandleResult(x => x < 5).HedgeAsync(3, TimeSpan.FromMilliseconds(100))
                .ExecuteAsync(async () =>
                {
                    await Task.Delay(50);

                    return invokeCount++;
                }));
        }

        [Fact]
        public async Task When_Cancel()
        {
            using (var cts = new CancellationTokenSource(500))
            {
                await Assert.ThrowsAsync<TaskCanceledException>(() =>
                    Policy<int>.HandleResult(x => x < 10).HedgeAsync(10, TimeSpan.FromMilliseconds(100))
                        .ExecuteAsync(async token =>
                        {
                            await Task.Delay(10000, token);

                            return 1;
                        }, cts.Token));
            }
        }
    }
}
