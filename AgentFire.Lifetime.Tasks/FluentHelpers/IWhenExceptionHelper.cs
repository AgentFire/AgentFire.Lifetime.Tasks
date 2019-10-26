using System.Threading;

namespace AgentFire.Lifetime.Tasks.FluentHelpers
{
    /// <summary />
    public interface IWhenExceptionHelper<out TOut, T>
    {
        /// <summary>
        /// Specify your asynchronous exception handler. The <see cref="CancellationToken"/> is triggered when there is no need to handle the exception any longer.
        /// </summary>
        TOut WhenExceptionAsync(AsyncExceptionHandler<T> exceptionHandler);

        /// <summary>
        /// Specify your synchronous exception handler.
        /// </summary>
        TOut WhenException(SyncExceptionHandler<T> exceptionHandler);
    }
}
