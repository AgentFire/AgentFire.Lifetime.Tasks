using System;
using System.Threading;
using System.Threading.Tasks;

namespace AgentFire.Lifetime.Tasks.FluentHelpers
{
    public interface IFor<T> : IFluentInterface
    {
        IBuilderWithInitialDegreeOfParallelismWhenException<T> Each(Func<T, CancellationToken, Task> method);
    }
}
