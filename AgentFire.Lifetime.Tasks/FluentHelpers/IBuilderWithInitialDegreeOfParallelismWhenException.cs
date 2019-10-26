namespace AgentFire.Lifetime.Tasks.FluentHelpers
{
    /// <summary />
    public interface IBuilderWithInitialDegreeOfParallelismWhenException<T> : IBuilder<T>, IFluentInterface, IWhenExceptionHelper<IBuilderWithInitialDegreeOfParallelism<T>, T>, IWithInitialDegreeOfParallelismHelper<IBuilderWhenException<T>, T>
    {
    }
}
