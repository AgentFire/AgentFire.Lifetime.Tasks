namespace AgentFire.Lifetime.Tasks
{
    /// <summary>
    /// Defines a way to stop <see cref="ForEach.ForEachAsync"/> execution.
    /// </summary>
    /// <remarks>
    /// For some tasty Retry behaviours, see Polly nuget package.
    /// </remarks>
    public enum ExceptionResolution
    {
        /// <summary>
        /// The exception is swallowed, the method continues running.
        /// </summary>
        Swallow = 0,

        /// <summary>
        /// No new tasks are to be created, all running are awaited to finish.
        /// </summary>
        SoftStop,

        /// <summary>
        /// No new tasks are to be created, all running are cancelled and awaited to finish.
        /// </summary>
        HardStop,

        /// <summary>
        /// No new tasks are to be created, all running are cancelled and NOT awaited to finish.
        /// </summary>
        Forget,

        /// <summary>
        /// No new tasks are to be created, all running are NOT cancelled and NOT awaited to finish.
        /// </summary>
        Abandon
    }
}
