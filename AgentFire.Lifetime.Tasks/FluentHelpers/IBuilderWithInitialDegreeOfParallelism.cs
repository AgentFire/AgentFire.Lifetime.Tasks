﻿namespace AgentFire.Lifetime.Tasks.FluentHelpers
{
    public interface IBuilderWithInitialDegreeOfParallelism<T> : IBuilder<T>, IWithInitialDegreeOfParallelism<T>, IFluentInterface
    {
    }
}
