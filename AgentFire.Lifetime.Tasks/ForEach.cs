using AgentFire.Lifetime.Tasks.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AgentFire.Lifetime.Tasks
{
    public sealed class ForEach<T>
    {
        // TODO: Make more sync roots, but that takes a bigger brain.
        private readonly object _syncRoot = new object();

        private readonly IEnumerable<T> _collection;
        private readonly Func<T, CancellationToken, Task> _method;
        private readonly Func<T, Exception, Task<ExceptionResolution>> _exceptionHandler;

        private readonly AsyncCounterGate _gate;

        /// <summary>
        /// Gets (can be changed in mid-process) a value of how many tasks are run at any given point of time.
        /// </summary>
        public int CurrentDegreeOfParallelism => _gate.Maximum;

        private readonly RunStatsImpl _stats = new RunStatsImpl();
        public IRunStatistics RunStats => _stats;

        /// <summary>
        /// Increases the maximum number of simultaneous tasks.
        /// This method is thread-safe.
        /// </summary>
        /// <param name="count">More than 0.</param>
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="InvalidOperationException" />
        public void IncreaseParallelism(int count)
        {
            #region Failsafes

            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (CurrentDegreeOfParallelism + (long)count > int.MaxValue)
            {
                throw new InvalidOperationException($"You cannot increment {nameof(CurrentDegreeOfParallelism)} to a value more than int.MaxValue");
            }

            #endregion

            lock (_syncRoot)
            {
                _gate.Maximum += count;
            }
        }
        /// <summary>
        /// Decreases the maximum number of simultaneous tasks. Excess tasks are safely awaited to finish.
        /// This method is thread-safe.
        /// </summary>
        /// <param name="count">More than 0.</param>
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="InvalidOperationException" />
        public void DecreaseParallelism(int count)
        {
            #region Failsafes

            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (CurrentDegreeOfParallelism - (long)count <= 0)
            {
                throw new InvalidOperationException($"You cannot decrement {nameof(CurrentDegreeOfParallelism)} to a value 0 or less");
            }

            #endregion

            lock (_syncRoot)
            {
                _gate.Maximum -= count;
            }
        }

        #region WorkItem

        private sealed class WorkItem
        {
            public Task Task { get; set; }
            public CancellationTokenSource CTS { get; set; }
            public IDisposable GateReleaser { get; set; }
        }

        #endregion

        private readonly HashSet<WorkItem> _currentTasks = new HashSet<WorkItem>();

        internal ForEach(IEnumerable<T> collection, Func<T, CancellationToken, Task> method, int initialDegreeOfParallelism, Func<T, Exception, Task<ExceptionResolution>> exceptionHandler)
        {
            _collection = collection;
            _method = method;
            _exceptionHandler = exceptionHandler;

            _gate = new AsyncCounterGate(initialDegreeOfParallelism);
        }

        private bool _isRunning = false;

        /// <summary>
        /// Main thingy.
        /// </summary>
        /// <remarks>
        /// This method will not run in parallel with itself.
        /// </remarks>
        /// <exception cref="AggregateException">
        /// Thrown when: An exception occurs on your processor method and: Either (1) your ExceptionHandler throws an exception, or (2) you did not provide ExceptionHandler.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when: your <see cref="CancellationToken"/> is triggered..
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when: you attempt to run this method in parallel.
        /// Or: the main <see cref="IEnumerable{T}"/> collection was modified in mid-iteration.
        /// </exception>
        public async Task<RunResult> Run(CancellationToken token)
        {
            #region Failsafes

            lock (_syncRoot)
            {
                if (_isRunning)
                {
                    throw new InvalidOperationException("You cannot parallel this method.");
                }

                _isRunning = true;
            }

            #endregion

            try
            {
                ExceptionResolution resolution = ExceptionResolution.Swallow;
                List<Exception> unhandledExceptions = new List<Exception>();

                #region Main Cycle

                foreach (T item in _collection)
                {
                    IDisposable gateReleaser = await _gate.WaitAsync(token).ConfigureAwait(false);

                    WorkItem workItem = new WorkItem
                    {
                        CTS = CancellationTokenSource.CreateLinkedTokenSource(token),
                        GateReleaser = gateReleaser
                    };

                    lock (_syncRoot)
                    {
                        _currentTasks.Add(workItem);
                    }

                    workItem.Task = Task.Run(async () =>
                    {
                        Interlocked.Increment(ref _stats._started);

                        try
                        {
                            await _method(item, workItem.CTS.Token).ConfigureAwait(false);
                            Interlocked.Increment(ref _stats._completed);
                        }
                        catch (OperationCanceledException) when (token.IsCancellationRequested)
                        {
                            // The user should expect this exception to be thrown when they trigger their token.
                            Interlocked.Increment(ref _stats._canceled);
                        }
                        catch (OperationCanceledException) when (workItem.CTS.Token.IsCancellationRequested)
                        {
                            // We triggered the cancellation.
                            Interlocked.Increment(ref _stats._canceled);
                        }
                        catch (Exception ex)
                        {
                            Interlocked.Increment(ref _stats._failed);

                            if (_exceptionHandler == null)
                            {
                                lock (_syncRoot)
                                {
                                    unhandledExceptions.Add(ex);
                                }
                            }
                            else
                            {
                                try
                                {
                                    ExceptionResolution r = await _exceptionHandler(item, ex).ConfigureAwait(false);

                                    Interlocked.Increment(ref _stats._exceptionsCaught);

                                    lock (_syncRoot)
                                    {
                                        resolution = (ExceptionResolution)Math.Max((int)resolution, (int)r);
                                    }

                                    if (r == ExceptionResolution.Swallow)
                                    {
                                        Interlocked.Increment(ref _stats._exceptionsSwallowed);
                                    }
                                }
                                catch (Exception resEx)
                                {
                                    lock (_syncRoot)
                                    {
                                        unhandledExceptions.Add(resEx);
                                    }
                                }
                            }
                        }
                        finally
                        {
                            // Some probably avoidable heavy stuff.
                            lock (_syncRoot)
                            {
                                _currentTasks.Remove(workItem);
                                workItem.GateReleaser.Dispose();
                                workItem.CTS.Dispose();
                            }
                        }
                    }, token);

                    token.ThrowIfCancellationRequested();

                    if (unhandledExceptions.Count > 0 || resolution != ExceptionResolution.Swallow)
                    {
                        break;
                    }
                }

                #endregion

                token.ThrowIfCancellationRequested();

                #region Dealing with uncaught exceptions

                lock (_syncRoot)
                {
                    if (unhandledExceptions.Count > 0)
                    {
                        throw new AggregateException(unhandledExceptions);
                    }
                }

                #endregion

                // Copy the value so that Exceptions thrown within processors do not change this value.
                ExceptionResolution finalResolution = resolution;

                #region Dealing with caught exceptions

                if (finalResolution != ExceptionResolution.Swallow)
                {
                    if (finalResolution != ExceptionResolution.Abandon)
                    {
                        if (finalResolution != ExceptionResolution.SoftStop)
                        {
                            lock (_syncRoot)
                            {
                                foreach (WorkItem workItem in _currentTasks.ToArray())
                                {
                                    workItem.CTS.Cancel();
                                }
                            }
                        }

                        if (finalResolution != ExceptionResolution.Forget)
                        {
                            await AwaitRemainingTasks(token).ConfigureAwait(false);
                        }
                    }

                    lock (_syncRoot)
                    {
                        if (unhandledExceptions.Count > 0)
                        {
                            throw new AggregateException(unhandledExceptions);
                        }
                    }

                    return RunResult.Interrupted;
                }

                #endregion

                await AwaitRemainingTasks(token).ConfigureAwait(false);
            }
            finally
            {
                _isRunning = false;
            }

            return RunResult.Finished;
        }

        private async Task AwaitRemainingTasks(CancellationToken token)
        {
            Task[] tasksLeft;

            lock (_syncRoot)
            {
                tasksLeft = _currentTasks.Select(T => T.Task).ToArray();
            }

            if (tasksLeft.Length > 0)
            {
                TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

                using (token.Register(() => tcs.SetCanceled()))
                {
                    await Task.WhenAny(Task.WhenAll(tasksLeft), tcs.Task).ConfigureAwait(false);
                }
            }
        }
    }
}
