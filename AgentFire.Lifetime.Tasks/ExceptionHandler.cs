using System;
using System.Threading;
using System.Threading.Tasks;

namespace AgentFire.Lifetime.Tasks
{
    /// <summary>
    /// Your exception handler.
    /// </summary>
    public delegate Task<ExceptionResolution> AsyncExceptionHandler<T>(T item, Exception ex, CancellationToken token);

    /// <summary>
    /// Your exception handler.
    /// </summary>
    public delegate ExceptionResolution SyncExceptionHandler<T>(T item, Exception ex);
}
