using AgentFire.Lifetime.Tasks.FluentHelpers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AgentFire.Lifetime.Tasks
{
    public static class Builder
    {
        #region Impl

        private sealed class Impl<T> : IFor<T>, IBuilderWithInitialDegreeOfParallelismWhenException<T>, IBuilderWithInitialDegreeOfParallelism<T>, IBuilderWhenException<T>, IBuilder<T>, IWithMaxDegreeOfParallelism<T>, IWhenException<T>, IFluentInterface
        {
            public IEnumerable<T> Collection { get; }
            public Func<T, CancellationToken, Task> Method { get; private set; }
            public int InitialDegreeOfParallelism { get; private set; } = 1;
            public Func<T, Exception, Task<ExceptionResolution>> ExceptionHandler { get; private set; } = null;

            public Impl(IEnumerable<T> collection)
            {
                Collection = collection;
            }

            public IBuilderWithInitialDegreeOfParallelismWhenException<T> Each(Func<T, CancellationToken, Task> method)
            {
                Method = method ?? throw new ArgumentNullException(nameof(method));
                return this;
            }
            public IBuilderWhenException<T> WithInitialDegreeOfParallelism(int value)
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                InitialDegreeOfParallelism = value;
                return this;
            }
            public IBuilderWithInitialDegreeOfParallelism<T> WhenException(Func<T, Exception, Task<ExceptionResolution>> exceptionHandler)
            {
                ExceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
                return this;
            }

            IBuilder<T> IWithMaxDegreeOfParallelism<T>.WithInitialDegreeOfParallelism(int value)
            {
                return WithInitialDegreeOfParallelism(value);
            }
            IBuilder<T> IWhenException<T>.WhenException(Func<T, Exception, Task<ExceptionResolution>> exceptionHandler)
            {
                return WhenException(exceptionHandler);
            }


            public ForEach<T> Build()
            {
                return new ForEach<T>(Collection, Method, InitialDegreeOfParallelism, ExceptionHandler);
            }
        }

        #endregion

        public static IFor<T> For<T>(IEnumerable<T> collection)
        {
            return new Impl<T>(collection ?? throw new ArgumentNullException(nameof(collection)));
        }
    }
}
