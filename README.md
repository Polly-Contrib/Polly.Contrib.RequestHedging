# Polly.Contrib.RequestHedging

Polly.Contrib.RequestHedginge allow the sequential and delayed execution of a set of tasks, until one of them completes successfully or until all of them are completed.
First task that is successfully completed triggers the cancellation and disposal of all other tasks and/or adjacent allocated resources.

# Installing via NuGet

    Install-Package Polly.Contrib.RequestHedging

# Usage

``` C#
PolicyBuilder[<TResult>] policyBuilder;

policyBuilder.HedgeAsync(maxAttemptCount: 2,
            hedgingDelay: TimeSpan.FromMilliseconds(100),
            onHedgeAsync: context => Task.CompletedTask);
```
