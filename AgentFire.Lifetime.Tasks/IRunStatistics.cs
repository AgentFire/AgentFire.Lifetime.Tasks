namespace AgentFire.Lifetime.Tasks
{
    public interface IRunStatistics
    {
        void Reset();

        int Started { get; }
        int Completed { get; }
        int Canceled { get; }
        int Failed { get; }
        int ExceptionsCaught { get; }
        int ExceptionsSwallowed { get; }
    }
}
