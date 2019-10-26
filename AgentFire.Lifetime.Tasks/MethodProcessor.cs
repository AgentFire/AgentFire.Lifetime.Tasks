using System.Threading;
using System.Threading.Tasks;

namespace AgentFire.Lifetime.Tasks
{
    /// <summary>
    /// Your main processor
    /// </summary>
    public delegate Task MethodProcessor<T>(T item, CancellationToken token);
}
