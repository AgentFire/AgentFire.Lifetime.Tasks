using Nito.AsyncEx;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AgentFire.Lifetime.Tasks.Tools
{
    public sealed class AsyncCounterGate
    {
        #region Releaser

        private sealed class Releaser : IDisposable
        {
            private readonly AsyncCounterGate _gate;

            public Releaser(AsyncCounterGate gate)
            {
                _gate = gate;
            }

            #region IDisposable Support
            private bool disposedValue = false; // To detect redundant calls

            void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {

                        _gate.Decrement();
                        // TODO: dispose managed state (managed objects).
                    }

                    // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                    // TODO: set large fields to null.

                    disposedValue = true;
                }
            }

            // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
            // ~Releaser()
            // {
            //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            //   Dispose(false);
            // }

            // This code added to correctly implement the disposable pattern.
            public void Dispose()
            {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                Dispose(true);
                // TODO: uncomment the following line if the finalizer is overridden above.
                // GC.SuppressFinalize(this);
            }
            #endregion

        }

        #endregion

        private readonly object _syncRoot = new object();
        private readonly AsyncManualResetEvent _ev = new AsyncManualResetEvent(true);
        private int _maximum = 0;

        public int Current { get; private set; } = 0;

        public int Maximum
        {
            get => _maximum;
            set
            {
                #region Failsafes

                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                #endregion

                lock (_syncRoot)
                {
                    _maximum = value;
                    ManageEvent();
                }
            }
        }

        public AsyncCounterGate(int initialMaximum)
        {
            Maximum = initialMaximum;
        }

        private void ManageEvent()
        {
            if (Current >= _maximum && _ev.IsSet)
            {
                _ev.Reset();
            }
            else if (Current < _maximum && !_ev.IsSet)
            {
                _ev.Set();
            }
        }

        private bool TryIncrement()
        {
            lock (_syncRoot)
            {
                if (Current >= Maximum)
                {
                    return false;
                }

                Current++;
                ManageEvent();

                return true;
            }
        }
        private void Decrement()
        {
            lock (_syncRoot)
            {
                Current--;
                ManageEvent();
            }
        }

        public async Task<IDisposable> WaitAsync(CancellationToken token)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();

                if (TryIncrement())
                {
                    return new Releaser(this);
                }

                await _ev.WaitAsync(token).ConfigureAwait(false);
            }
        }
    }
}
