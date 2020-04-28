using AgentFire.Lifetime.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tests
{
    [TestClass]
    public class ForEachTests
    {
        private readonly Stopwatch _sw = Stopwatch.StartNew();

        [TestMethod]
        public void Init()
        {
            Assert.ThrowsException<ArgumentNullException>(() => Builder.For((IEnumerable<object>)null));

            var b = Builder.For(new int[] { 1, 2, 3 });

            for (int i = 0; i < 100; i++)
            {
                b = Builder.For(new int[] { 1, 2, 3 });
            }

            Assert.ThrowsException<ArgumentNullException>(() => b.Each(null));

            var e = b.Each((_, __) => Task.CompletedTask);

            Assert.ThrowsException<ArgumentNullException>(() => e.WhenException(null));
            Assert.ThrowsException<ArgumentNullException>(() => e.WhenExceptionAsync(null));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => e.WithInitialDegreeOfParallelism(0));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => e.WithInitialDegreeOfParallelism(-1));

            var instance = e.Build();

            Assert.ThrowsException<InvalidOperationException>(() => instance.DecreaseParallelism(int.MaxValue));
            Assert.ThrowsException<InvalidOperationException>(() => instance.IncreaseParallelism(int.MaxValue));

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => instance.DecreaseParallelism(-1));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => instance.IncreaseParallelism(-1));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => instance.DecreaseParallelism(0));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => instance.IncreaseParallelism(0));
        }

        [TestMethod]
        public async Task Basic()
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            ForEach<int> _forEach = Builder
                .For(new int[] { 1, 2, 3 })
                .Each((T, token) => Task.Delay(T * 100, token).ContinueWith(_ => tcs.Task))
                .Build();

            Task<RunResult> t = _forEach.Run(CancellationToken.None);

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => _forEach.Run(CancellationToken.None));

            tcs.SetResult(true);

            RunResult result = await t;

            Assert.AreEqual(RunResult.Finished, result);
            Assert.IsTrue(_sw.ElapsedMilliseconds >= 600);
        }

        [TestMethod]
        public async Task BasicCancellation()
        {
            int started = 0;

            ForEach<int> _forEach = Builder
                .For(new int[] { 1, 2, 3, 4, 5 })
                .Each(async (T, token) =>
                {
                    Interlocked.Increment(ref started);

                    try
                    {
                        await Task.Delay(1000, token);
                    }
                    finally
                    {
                        Assert.IsTrue(token.IsCancellationRequested);
                    }

                    Assert.Fail();
                })
                .WithInitialDegreeOfParallelism(4)
                .WhenException((_, ex) =>
                {
                    Assert.Fail();
                    return ExceptionResolution.Abandon;
                })
                .Build();

            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                cts.CancelAfter(500);

                try
                {
                    await _forEach.Run(cts.Token);
                    Assert.Fail();
                }
                catch (OperationCanceledException ex)
                {
                    Assert.AreEqual(ex.CancellationToken, cts.Token);
                }
            }

            Assert.AreEqual(_forEach.CurrentDegreeOfParallelism, started);
            await Task.Delay(1500);
            Assert.AreEqual(_forEach.CurrentDegreeOfParallelism, started);
        }

        [TestMethod]
        public async Task Swallow()
        {
            int swallowed = 0;

            ForEach<int> _forEach = Builder
                .For(new int[] { 1, 2, 3, 4, 5, 6 })
                .Each((T, token) => throw new Exception())
                .WhenException((T, ex) =>
                {
                    Interlocked.Increment(ref swallowed);
                    return ExceptionResolution.Swallow;
                })
                .Build();


            await _forEach.Run(CancellationToken.None);

            Assert.AreEqual(6, swallowed);
            Assert.AreEqual(6, _forEach.RunStats.ExceptionsCaught);
            Assert.AreEqual(6, _forEach.RunStats.ExceptionsSwallowed);
        }

        [TestMethod]
        public async Task Parallelization()
        {
            bool f1 = false;

            ForEach<int> _forEach = Builder
                .For(new int[] { 1, 2, 3 })
                .Each(async (T, token) =>
                {
                    if (T == 1)
                    {
                        await Task.Delay(200);
                        f1 = true;
                    }

                    if (T == 2)
                    {
                        await Task.Delay(100);
                    }

                    if (T == 3)
                    {
                        Assert.IsTrue(f1);
                    }
                })
                .WithInitialDegreeOfParallelism(2)
                .Build();

            RunResult result = await _forEach.Run(CancellationToken.None);

            Assert.AreEqual(RunResult.Finished, result);
        }

        [TestMethod]
        public async Task SoftStop()
        {
            ForEach<int> _forEach = Builder
                .For(Enumerable.Range(1, 100))
                .Each((T, token) =>
                {
                    if (T == 50)
                    {
                        throw new Exception();
                    }

                    return Task.Delay(100, token);
                })
                .WithInitialDegreeOfParallelism(10)
                .WhenException((T, ex) => ExceptionResolution.SoftStop)
                .Build();

            RunResult result = await _forEach.Run(CancellationToken.None);

            Assert.AreEqual(RunResult.Interrupted, result);
            Assert.IsTrue(_forEach.RunStats.Started <= 60);
            Assert.IsTrue(_forEach.RunStats.Completed >= 40);
        }

        [TestMethod]
        public async Task HardStop()
        {
            bool hardStop = false;

            ForEach<int> _forEach = Builder
                .For(Enumerable.Range(1, 100))
                .Each(async (T, token) =>
                {
                    if (T == 50)
                    {
                        throw new Exception();
                    }

                    try
                    {
                        await Task.Delay(100, token);
                    }
                    catch (OperationCanceledException)
                    {
                        Assert.IsTrue(hardStop);
                    }
                })
                .WithInitialDegreeOfParallelism(10)
                .WhenException((T, ex) =>
                {
                    hardStop = true;
                    return ExceptionResolution.HardStop;
                })
                .Build();

            RunResult result = await _forEach.Run(CancellationToken.None);

            Assert.AreEqual(RunResult.Interrupted, result);
            Assert.IsTrue(_forEach.RunStats.Started <= 60);
            Assert.IsTrue(_forEach.RunStats.Completed >= 40);
        }

        [TestMethod]
        public async Task Forget()
        {
            ForEach<int> _forEach = Builder
                .For(Enumerable.Range(1, 100))
                .Each(async (T, token) =>
                {
                    if (T == 50)
                    {
                        throw new Exception();
                    }

                    try
                    {
                        await Task.Delay(100, token);
                    }
                    catch (OperationCanceledException)
                    {
                        Assert.Fail();
                    }
                })
                .WithInitialDegreeOfParallelism(10)
                .WhenException((T, ex) => ExceptionResolution.Forget)
                .Build();

            RunResult result = await _forEach.Run(CancellationToken.None);

            Assert.AreEqual(RunResult.Interrupted, result);
            Assert.IsTrue(_forEach.RunStats.Started <= 60);
            Assert.IsTrue(_forEach.RunStats.Completed >= 40);
        }

        [TestMethod]
        public async Task UnhandledExceptions1()
        {
            Exception testEx = new Exception();

            ForEach<int> _forEach = Builder
                .For(Enumerable.Range(1, 100))
                .Each((T, token) =>
                {
                    if (T == 50)
                    {
                        throw testEx;
                    }

                    return Task.Delay(10, token);
                })
                .WithInitialDegreeOfParallelism(10)
                .Build();

            AggregateException ex = await Assert.ThrowsExceptionAsync<AggregateException>(() => _forEach.Run(CancellationToken.None));

            Assert.AreEqual(testEx, ex.InnerExceptions.Single());

            Assert.IsTrue(_forEach.RunStats.Completed >= 40);
            Assert.IsTrue(_forEach.RunStats.Started <= 60);
        }

        [TestMethod]
        public async Task UnhandledExceptions2()
        {
            Exception testEx = new Exception();
            Exception testEx2 = new Exception();

            ForEach<int> _forEach = Builder
                .For(Enumerable.Range(1, 100))
                .Each((T, token) =>
                {
                    if (T == 50)
                    {
                        throw testEx;
                    }

                    return Task.Delay(10, token);
                })
                .WithInitialDegreeOfParallelism(10)
                .WhenException((_, ex) =>
                {
                    Assert.AreEqual(testEx, ex);
                    throw testEx2;
                })
                .Build();

            AggregateException aggr = await Assert.ThrowsExceptionAsync<AggregateException>(() => _forEach.Run(CancellationToken.None));

            Assert.AreEqual(testEx2, aggr.InnerExceptions.Single());

            Assert.IsTrue(_forEach.RunStats.Completed >= 40);
            Assert.IsTrue(_forEach.RunStats.Started <= 60);
        }

        [TestMethod]
        public async Task ParallelismVariance()
        {
            ForEach<int> _forEach = Builder
                .For(Enumerable.Range(1, 100))
                .Each((T, token) => Task.Delay(100))
                .WithInitialDegreeOfParallelism(1)
                .Build();

            var t = _forEach.Run(CancellationToken.None);

            var t1 = Task.Run(async () =>
            {
                for (int i = 0; i < 10; i++)
                {
                    _forEach.IncreaseParallelism(1);
                    await Task.Delay(50);
                }
            });

            var t2 = Task.Run(async () =>
            {
                for (int i = 1; i <= 10; i++)
                {
                    _forEach.IncreaseParallelism(i);
                    await Task.Delay(50);
                }
            });

            var t3 = Task.Run(async () =>
            {
                for (int i = 0; i < 10; i++)
                {
                    _forEach.DecreaseParallelism(1);
                    await Task.Delay(50);
                }
            });

            RunResult result = await t;

            Assert.AreEqual(RunResult.Finished, result);

            await Task.WhenAll(t1, t2, t3);

            Assert.AreEqual(56, _forEach.CurrentDegreeOfParallelism);
        }

        class A { }

        [TestMethod]
        public async Task ReferenceSafety()
        {
            A a1 = new A();
            A a2 = new A();
            A a3 = new A();

            HashSet<A> hs = new HashSet<A>();

            await Builder
                .For(new A[] { a1, a2, a3 })
                .Each(async (T, token) =>
                {
                    await Task.Delay(100);

                    lock (hs)
                    {
                        hs.Add(T);
                    }
                })
                .WithInitialDegreeOfParallelism(100)
                .Build()
                .Run(CancellationToken.None);

            Assert.AreEqual(3, hs.Count);
            Assert.IsTrue(hs.Contains(a1));
            Assert.IsTrue(hs.Contains(a2));
            Assert.IsTrue(hs.Contains(a3));
        }
    }
}
