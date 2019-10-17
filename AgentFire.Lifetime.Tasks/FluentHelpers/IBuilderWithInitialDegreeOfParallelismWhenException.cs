using System;
using System.Threading.Tasks;

namespace AgentFire.Lifetime.Tasks.FluentHelpers
{
    public interface IBuilderWithInitialDegreeOfParallelismWhenException<T> : IBuilder<T>, IFluentInterface
    {
        IBuilderWhenException<T> WithInitialDegreeOfParallelism(int value);
        IBuilderWithInitialDegreeOfParallelism<T> WhenException(Func<T, Exception, Task<ExceptionResolution>> exceptionHandler);
    }
}
