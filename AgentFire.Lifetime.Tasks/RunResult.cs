namespace AgentFire.Lifetime.Tasks
{
    public enum RunResult
    {
        /// <summary>
        /// All operations completed.
        /// </summary>
        Finished,

        /// <summary>
        /// One or more operations returned <see cref="ExceptionResolution.SoftStop"/> or higher in their ExceptionHandler, which resulted in cancellation.
        /// </summary>
        Interrupted
    }
}
