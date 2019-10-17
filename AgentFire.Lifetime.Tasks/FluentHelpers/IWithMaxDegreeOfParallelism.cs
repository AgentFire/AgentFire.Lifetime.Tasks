namespace AgentFire.Lifetime.Tasks.FluentHelpers
{
    public interface IWithMaxDegreeOfParallelism<T> : IFluentInterface
    {
        IBuilder<T> WithInitialDegreeOfParallelism(int value);
    }
}
