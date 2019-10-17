namespace AgentFire.Lifetime.Tasks.FluentHelpers
{
    public interface IBuilder<T> : IFluentInterface
    {
        ForEach<T> Build();
    }
}
