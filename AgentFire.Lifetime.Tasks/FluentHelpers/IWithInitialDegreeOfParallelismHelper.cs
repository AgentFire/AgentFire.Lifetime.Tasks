namespace AgentFire.Lifetime.Tasks.FluentHelpers
{
    /// <summary />
    public interface IWithInitialDegreeOfParallelismHelper<out TOut, T>
    {
        /// <summary>
        /// Specify your initial simultaneous tasks cound during iteration.
        /// </summary>
        TOut WithInitialDegreeOfParallelism(int value);
    }
}
