namespace AsyncWork.Utilities
{
    /// <summary>
    /// An async-compatible manual-reset event.
    /// </summary>
    public sealed class AsyncManualResetEvent
    {
        /// <summary>
        /// Lock to protect access to <see cref="_tcs"/>.
        /// </summary>
        private readonly object _mutex;

        /// <summary>
        /// The current state of the event.
        /// </summary>
        /// <remarks>All access to this object has to be protected by <see cref="_mutex"/>.</remarks>
        private TaskCompletionSource<object> _tcs;

        /// <summary>
        /// Creates an async-compatible manual-reset event.
        /// </summary>
        /// <param name="set">Whether the manual-reset event is initially set or unset.</param>
        public AsyncManualResetEvent(bool set)
        {
            this._mutex = new object();
            this._tcs = this.CreateAsyncTaskSource<object>();
            if (set)
                this._tcs.TrySetResult(null);
        }

        /// <summary>
        /// Creates an async-compatible manual-reset event that is initially unset.
        /// </summary>
        public AsyncManualResetEvent()
            : this(false)
        {
        }

        /// <summary>
        /// Whether this event is currently set. This member is seldom used; code using this member has a high possibility of race conditions.
        /// </summary>
        public bool IsSet
        {
            get
            {
                lock (this._mutex)
                {
                    return this._tcs.Task.IsCompleted;
                }
            }
        }

        /// <summary>
        /// Asynchronously waits for this event to be set.
        /// </summary>
        public Task WaitAsync()
        {
            lock (this._mutex)
            {
                return this._tcs.Task;
            }
        }

        /// <summary>
        /// Asynchronously waits for this event to be set or for the wait to be canceled.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to cancel the wait. If this token is already canceled, this method will first check whether the event is set.</param>
        public async Task WaitAsync(CancellationToken cancellationToken)
        {
            Task waitTask = this.WaitAsync();
            if (waitTask.IsCompleted)
                return;

            if (!cancellationToken.CanBeCanceled)
            {
                await waitTask.ConfigureAwait(false);
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var tcs = new TaskCompletionSource<object>();
            using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken), useSynchronizationContext: false))
            {
                await await Task.WhenAny(waitTask, tcs.Task).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Sets the event, atomically completing every task returned by <see cref="AsyncManualResetEvent.WaitAsync()"/>. If the event is already set, this method does nothing.
        /// </summary>
        public void Set()
        {
            lock (this._mutex)
            {
                this._tcs.TrySetResult(null);
            }
        }

        /// <summary>
        /// Resets the event. If the event is already reset, this method does nothing.
        /// </summary>
        public void Reset()
        {
            lock (this._mutex)
            {
                if (this._tcs.Task.IsCompleted)
                    this._tcs = this.CreateAsyncTaskSource<object>();
            }
        }

        /// <summary>
        /// Creates a new TCS for use with async code, and which forces its continuations to execute asynchronously.
        /// </summary>
        /// <typeparam name="TResult">The type of the result of the TCS.</typeparam>
        private TaskCompletionSource<TResult> CreateAsyncTaskSource<TResult>()
        {
            return new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }
}
