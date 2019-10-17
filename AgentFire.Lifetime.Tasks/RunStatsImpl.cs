namespace AgentFire.Lifetime.Tasks
{
    internal sealed class RunStatsImpl : IRunStatistics
    {
        public int _started = 0;
        public int _completed = 0;
        public int _canceled = 0;
        public int _failed = 0;
        public int _exceptionsCaught = 0;
        public int _exceptionsSwallowed = 0;

        public int Started => _started;
        public int Completed => _completed;
        public int Canceled => _canceled;
        public int Failed => _failed;
        public int ExceptionsCaught => _exceptionsCaught;
        public int ExceptionsSwallowed => _exceptionsSwallowed;

        public void Reset()
        {
            _started = 0;
            _completed = 0;
            _canceled = 0;
            _failed = 0;
            _exceptionsCaught = 0;
            _exceptionsSwallowed = 0;
        }
    }
}
