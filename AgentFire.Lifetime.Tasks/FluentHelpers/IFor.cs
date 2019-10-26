using System;
using System.Threading;
using System.Threading.Tasks;

namespace AgentFire.Lifetime.Tasks.FluentHelpers
{
    /// <summary />
    public interface IFor<T> : IFluentInterface
    {
        /// <summary>
        /// Specify your main processor method.
        /// </summary>
        IBuilderWithInitialDegreeOfParallelismWhenException<T> Each(MethodProcessor<T> method);
    }
}
