using AgentFire.Lifetime.Tasks.FluentHelpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgentFire.Lifetime.Tasks
{
    /// <summary>
    /// The builder Builder for ForEach.
    /// </summary>
    public static class Builder
    {
        private sealed class Impl<T> : IFor<T>, IBuilderWithInitialDegreeOfParallelismWhenException<T>, IBuilderWithInitialDegreeOfParallelism<T>, IBuilderWhenException<T>, IBuilder<T>, IWithInitialDegreeOfParallelism<T>, IWhenException<T>, IFluentInterface
        {
            public IEnumerable<T> Collection { get; }
            public MethodProcessor<T> Method { get; private set; }
            public int InitialDegreeOfParallelism { get; private set; } = 1;
            public AsyncExceptionHandler<T> ExceptionHandler { get; private set; } = null;

            public Impl(IEnumerable<T> collection)
            {
                Collection = collection;
            }

            public IBuilderWithInitialDegreeOfParallelismWhenException<T> Each(MethodProcessor<T> method)
            {
                Method = method ?? throw new ArgumentNullException(nameof(method));
                return this;
            }

            public Impl<T> WithInitialDegreeOfParallelism(int value)
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                InitialDegreeOfParallelism = value;
                return this;
            }

            IBuilder<T> IWithInitialDegreeOfParallelismHelper<IBuilder<T>, T>.WithInitialDegreeOfParallelism(int value)
            {
                return WithInitialDegreeOfParallelism(value);
            }
            IBuilderWhenException<T> IWithInitialDegreeOfParallelismHelper<IBuilderWhenException<T>, T>.WithInitialDegreeOfParallelism(int value)
            {
                return WithInitialDegreeOfParallelism(value);
            }

            private Impl<T> WhenException(AsyncExceptionHandler<T> exceptionHandler)
            {
                ExceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
                return this;
            }
            private Impl<T> WhenException(SyncExceptionHandler<T> exceptionHandler)
            {
                if (exceptionHandler is null)
                {
                    throw new ArgumentNullException(nameof(exceptionHandler));
                }

                return WhenException((item, ex, _) => Task.FromResult(exceptionHandler(item, ex)));
            }

            public ForEach<T> Build()
            {
                return new ForEach<T>(Collection, Method, InitialDegreeOfParallelism, ExceptionHandler);
            }

            IBuilderWithInitialDegreeOfParallelism<T> IWhenExceptionHelper<IBuilderWithInitialDegreeOfParallelism<T>, T>.WhenExceptionAsync(AsyncExceptionHandler<T> exceptionHandler)
            {
                return WhenException(exceptionHandler);
            }
            IBuilder<T> IWhenExceptionHelper<IBuilder<T>, T>.WhenExceptionAsync(AsyncExceptionHandler<T> exceptionHandler)
            {
                return WhenException(exceptionHandler);
            }
            IBuilder<T> IWhenExceptionHelper<IBuilder<T>, T>.WhenException(SyncExceptionHandler<T> exceptionHandler)
            {
                return WhenException(exceptionHandler);
            }
            IBuilderWithInitialDegreeOfParallelism<T> IWhenExceptionHelper<IBuilderWithInitialDegreeOfParallelism<T>, T>.WhenException(SyncExceptionHandler<T> exceptionHandler)
            {
                return WhenException(exceptionHandler);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static IFor<T> For<T>(IEnumerable<T> collection)
        {
            return new Impl<T>(collection ?? throw new ArgumentNullException(nameof(collection)));
        }
    }
}
