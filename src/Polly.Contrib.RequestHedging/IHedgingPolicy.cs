namespace Polly.Contrib.RequestHedging
{
    /// <summary>
    /// Defines properties and methods common to all Hedging policies.
    /// </summary>

    public interface IHedgingPolicy : IsPolicy
    {
    }

    /// <summary>
    /// Defines properties and methods common to all Hedging policies generic-typed for executions returning results of type <typeparamref name="TResult"/>.
    /// </summary>
    public interface IHedgingPolicy<TResult> : IHedgingPolicy
    {

    }
}
