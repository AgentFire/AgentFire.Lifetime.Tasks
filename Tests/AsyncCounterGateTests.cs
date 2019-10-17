using System;
using System.Threading;
using System.Threading.Tasks;
using AgentFire.Lifetime.Tasks.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class AsyncCounterGateTests
    {
        private readonly AsyncCounterGate _gate = new AsyncCounterGate(3);

        [TestMethod]
        public void Init()
        {
            Assert.AreEqual(0, _gate.Current);
            Assert.AreEqual(3, _gate.Maximum);

            _gate.Maximum = 2;

            Assert.AreEqual(2, _gate.Maximum);

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => _gate.Maximum = 0);
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => _gate.Maximum = -1);
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => _gate.Maximum = int.MinValue);
        }

        [TestMethod]
        public async Task BasicUsage()
        {
            Assert.AreEqual(0, _gate.Current);

            using (await _gate.WaitAsync(CancellationToken.None))
            {
                Assert.AreEqual(1, _gate.Current);

                using (await _gate.WaitAsync(CancellationToken.None))
                {
                    Assert.AreEqual(2, _gate.Current);

                    using (await _gate.WaitAsync(CancellationToken.None))
                    {
                        Assert.AreEqual(3, _gate.Current);
                    }

                    Assert.AreEqual(2, _gate.Current);
                }

                Assert.AreEqual(1, _gate.Current);
            }

            Assert.AreEqual(0, _gate.Current);
        }

        [TestMethod]
        public async Task Cancellations()
        {
            using (await _gate.WaitAsync(CancellationToken.None))
            using (await _gate.WaitAsync(CancellationToken.None))
            using (await _gate.WaitAsync(CancellationToken.None))
            using (CancellationTokenSource cts = new CancellationTokenSource(500))
            {
                Assert.AreEqual(3, _gate.Current);

                await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
                {
                    using (await _gate.WaitAsync(cts.Token))
                    {
                        Assert.Fail();
                    }
                });

                Assert.AreEqual(3, _gate.Current);
            }
        }

        [TestMethod]
        public async Task MaximumVariance()
        {
            Assert.AreEqual(3, _gate.Maximum);
            _gate.Maximum = 1;
            Assert.AreEqual(1, _gate.Maximum);

            Assert.AreEqual(0, _gate.Current);
            using (await _gate.WaitAsync(CancellationToken.None))
            {
                Assert.AreEqual(1, _gate.Current);

                Task<IDisposable> t = _gate.WaitAsync(CancellationToken.None);

                Assert.AreEqual(1, _gate.Current);

                await Task.Delay(200);

                Assert.IsFalse(t.IsCompleted);
                Assert.AreEqual(1, _gate.Current);

                _gate.Maximum = 2;

                Assert.AreEqual(2, _gate.Maximum);

                await Task.Delay(200);

                Assert.AreEqual(2, _gate.Current);
                Assert.IsTrue(t.IsCompleted);

                (await t).Dispose();

                Assert.AreEqual(1, _gate.Current);

                (await t).Dispose(); // Double dispose test.

                Assert.AreEqual(1, _gate.Current);

                using (await _gate.WaitAsync(CancellationToken.None))
                {
                    Assert.AreEqual(2, _gate.Current);

                    _gate.Maximum = 1;

                    Assert.AreEqual(1, _gate.Maximum);
                    Assert.AreEqual(2, _gate.Current);
                }

                Assert.AreEqual(1, _gate.Current);
                Assert.AreEqual(1, _gate.Maximum);
            }

            Assert.AreEqual(0, _gate.Current);
            Assert.AreEqual(1, _gate.Maximum);
        }
    }
}
