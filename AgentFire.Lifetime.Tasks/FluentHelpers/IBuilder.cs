namespace AgentFire.Lifetime.Tasks.FluentHelpers
{
    /// <summary />
    public interface IBuilder<T> : IFluentInterface
    {
        /// <summary>
        /// Builds the <see cref="ForEach{T}"/> instance.
        /// </summary>
        ForEach<T> Build();
    }
}
