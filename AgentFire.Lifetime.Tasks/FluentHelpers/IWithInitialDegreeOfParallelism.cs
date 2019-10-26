namespace AgentFire.Lifetime.Tasks.FluentHelpers
{
    /// <summary />
    public interface IWithInitialDegreeOfParallelism<T> : IFluentInterface, IWithInitialDegreeOfParallelismHelper<IBuilder<T>, T>
    {
    }
}
