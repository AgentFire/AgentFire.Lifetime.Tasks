using System;
using System.Threading.Tasks;

namespace AgentFire.Lifetime.Tasks.FluentHelpers
{
    public interface IWhenException<T> : IFluentInterface
    {
        IBuilder<T> WhenException(Func<T, Exception, Task<ExceptionResolution>> exceptionHandler);
    }
}
