using AsyncWork.Abstract;
using Utilities;

namespace AsyncWork.Concrete
{
    /// <summary>
    /// Async queue is a thread-safe queue that can operate in callback mode or blocking dequeue mode.
    /// In callback mode it asynchronously executes a user-defined callback when a new item is added to the queue.
    /// In blocking dequeue mode, <see cref="DequeueAsync(CancellationToken)"/> is used to wait for and dequeue
    /// an item from the queue once it becomes available.
    /// <para>
    /// In callback mode, the queue guarantees that the user-defined callback is executed only once at the time.
    /// If another item is added to the queue, the callback is called again after the current execution
    /// is finished.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Type of items to be inserted in the queue.</typeparam>
    internal class AsyncQueue<T> : IDisposable, IAsyncQueue<T>, IAsyncDelegateDequeuer<T>
    {
        /// <summary>
        /// Execution context holding information about the current status of the execution
        /// in order to recognize if <see cref="Dispose"/> was called within the callback method.
        /// </summary>
        private class AsyncContext
        {
            /// <summary>
            /// Set to <c>true</c> if <see cref="Dispose"/> was called from within the callback routine,
            /// set to <c>false</c> otherwise.
            /// </summary>
            public bool DisposeRequested { get; set; }
        }

        /// <summary>
        /// Represents a callback method to be executed when a new item is added to the queue.
        /// </summary>
        /// <param name="item">Newly added item.</param>
        /// <param name="cancellationToken">Cancellation token that the callback method should use for its async operations to avoid blocking the queue during shutdown.</param>
        /// <remarks>It is allowed to call <see cref="Dispose"/> from the callback method.</remarks>
        public delegate Task OnEnqueueAsync(T item, CancellationToken cancellationToken);

        /// <summary>Lock object to protect access to <see cref="_items"/>.</summary>
        private readonly object _lockObject;

        /// <summary>Storage of items in the queue that are waiting to be consumed.</summary>
        /// <remarks>All access to this object has to be protected by <see cref="_lockObject"/>.</remarks>
        private readonly Queue<T> _items;

        /// <summary>Event that is triggered when at least one new item is waiting in the queue.</summary>
        private readonly AsyncManualResetEvent _signal;

        /// <summary>Callback routine to be called when a new item is added to the queue.</summary>
        private readonly OnEnqueueAsync _onEnqueueAsync;

        /// <summary>Consumer of the items in the queue which responsibility is to execute the user defined callback.</summary>
        /// <remarks>Internal for test purposes.</remarks>
        internal Task ConsumerTask { get; private set; }

        /// <summary>Cancellation that is triggered when the component is disposed.</summary>
        private readonly CancellationTokenSource _cancellationTokenSource;

        /// <summary>Number of pending dequeue operations which need to be finished before the queue can fully dispose.</summary>
        private volatile int _unfinishedDequeueCount;

        /// <summary><c>true</c> if <see cref="Dispose"/> was called, <c>false</c> otherwise.</summary>
        private bool _disposed;

        /// <summary><c>true</c> if the queue operates in callback mode, <c>false</c> if it operates in blocking dequeue mode.</summary>
        private readonly bool _callbackMode;

        /// <summary>
        /// Async context to allow to recognize whether <see cref="Dispose"/> was called from within the callback routine.
        /// <para>
        /// Is not <c>null</c> if the queue is operating in callback mode and the current async execution context is the one that executes the callbacks,
        /// set to <c>null</c> otherwise.
        /// </para>
        /// </summary>
        private readonly AsyncLocal<AsyncContext> _asyncContext;

        /// <summary>
        /// Initializes the queue either in blocking dequeue mode or in callback mode.
        /// </summary>
        /// <param name="onEnqueueAsync">Callback routine to be called when a new item is added to the queue, or <c>null</c> to operate in blocking dequeue mode.</param>
        internal AsyncQueue(OnEnqueueAsync onEnqueueAsync = null)
        {
            this._callbackMode = onEnqueueAsync != null;
            this._lockObject = new object();
            this._items = new Queue<T>();
            this._signal = new AsyncManualResetEvent();
            this._onEnqueueAsync = onEnqueueAsync;
            this._cancellationTokenSource = new CancellationTokenSource();
            this._asyncContext = new AsyncLocal<AsyncContext>();
            this.ConsumerTask = this._callbackMode ? this.ConsumerAsync() : null;
        }

        /// <summary>
        /// Add a new item to the queue and signal to the consumer task.
        /// </summary>
        /// <param name="item">Item to be added to the queue.</param>
        public void Enqueue(T item)
        {
            lock (this._lockObject)
            {
                this._items.Enqueue(item);
                this._signal.Set();
            }
        }

        /// <summary>
        /// The number of items in the queue.
        /// This property should only be used for collecting statistics.
        /// </summary>
        public int Count
        {
            get
            {
                lock (this._lockObject)
                {
                    return this._items.Count;
                }
            }
        }

        /// <summary>
        /// Consumer of the newly added items to the queue that waits for the signal
        /// and then executes the user-defined callback.
        /// <para>
        /// This consumer loop is only used when the queue is operating in the callback mode.
        /// </para>
        /// </summary>
        private async Task ConsumerAsync()
        {
            // Set the context, so that Dispose called from callback will recognize it.
            this._asyncContext.Value = new AsyncContext();

            bool callDispose = false;
            CancellationToken cancellationToken = this._cancellationTokenSource.Token;
            while (!callDispose && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Wait for an item to be enqueued.
                    await this._signal.WaitAsync(cancellationToken).ConfigureAwait(false);

                    // Dequeue all items and execute the callback.
                    T item;
                    while (this.TryDequeue(out item) && !cancellationToken.IsCancellationRequested)
                    {
                        await this._onEnqueueAsync(item, cancellationToken).ConfigureAwait(false);

                        if (this._asyncContext.Value.DisposeRequested)
                        {
                            callDispose = true;
                            break;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            if (callDispose) this.DisposeInternal(true);
        }

        /// <summary>
        /// Dequeues an item from the queue if there is one.
        /// If the queue is empty, the method waits until an item is available.
        /// </summary>
        /// <param name="cancellation">Cancellation token that allows aborting the wait if the queue is empty.</param>
        /// <returns>Dequeued item from the queue.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the cancellation token is triggered or when the queue is disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if this method is called on a queue that operates in callback mode.</exception>
        public async Task<T> DequeueAsync(CancellationToken cancellation = default(CancellationToken))
        {
            if (this._callbackMode)
                throw new InvalidOperationException($"{nameof(this.DequeueAsync)} called on queue in callback mode.");

            // Increment the counter so that the queue's cancellation source is not disposed when we are using it.
            Interlocked.Increment(ref this._unfinishedDequeueCount);

            try
            {
                if (this._disposed)
                    throw new OperationCanceledException();

                // First check if an item is available. If it is, just return it.
                T item;
                if (this.TryDequeue(out item))
                    return item;

                // If the queue is empty, we need to wait until there is an item available.
                using (CancellationTokenSource cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellation, this._cancellationTokenSource.Token))
                {
                    while (true)
                    {
                        await this._signal.WaitAsync(cancellationSource.Token).ConfigureAwait(false);

                        // Note that another thread could consume the message before us,
                        // so dequeue safely and loop if nothing is available.
                        if (this.TryDequeue(out item))
                            return item;
                    }
                }
            }
            finally
            {
                Interlocked.Decrement(ref this._unfinishedDequeueCount);
            }
        }

        /// <summary>
        /// Dequeues an item from the queue if there is any.
        /// </summary>
        /// <param name="item">If the function succeeds, this is filled with the dequeued item.</param>
        /// <returns><c>true</c> if an item was dequeued, <c>false</c> if the queue was empty.</returns>
        public bool TryDequeue(out T item)
        {
            item = default(T);
            lock (this._lockObject)
            {
                if (this._items.Count > 0)
                {
                    item = this._items.Dequeue();

                    if (this._items.Count == 0)
                        this._signal.Reset();

                    return true;
                }

                return false;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this._asyncContext.Value != null)
            {
                // We can't dispose now as we would end up in deadlock because we are called from within the callback.
                // We just mark that dispose was requested and process with the dispose once we return from the callback.
                this._asyncContext.Value.DisposeRequested = true;
                return;
            }

            this.DisposeInternal(false);
        }

        /// <summary>
        /// Frees resources used by the queue and only returns until all unfinished tasks of the objects are finished.
        /// </summary>
        /// <param name="calledFromConsumerTask"><c>true</c> if this method is called from <see cref="ConsumerTask"/>, <c>false</c> otherwise.</param>
        private void DisposeInternal(bool calledFromConsumerTask)
        {
            // We do not need synchronization over this, if it is going to be missed
            // we just wait a little longer.
            this._disposed = true;

            this._cancellationTokenSource.Cancel();
            if (!calledFromConsumerTask) this.ConsumerTask?.Wait();

            if (!this._callbackMode)
            {
                // Wait until all pending dequeue operations are finished.
                // As this is very fast once disposed has been set to true,
                // we can afford busy wait.
                while (this._unfinishedDequeueCount > 0)
                    Thread.Sleep(5);
            }

            this._cancellationTokenSource.Dispose();
        }
    }
}
