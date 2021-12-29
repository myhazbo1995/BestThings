using AsyncWork.Abstract;
using Utilities;
using Microsoft.Extensions.Logging;
using System.Text;

namespace AsyncWork.Concrete
{
    /// <summary>
    /// Provides functionality for creating and tracking asynchronous operations that happen in the background.
    /// </summary>
    public partial class AsyncProvider : IAsyncProvider
    {
        private const int _defaultLoopRepeatInterval = 1000;

        private readonly object _lockAsyncDelegates;
        private readonly object _lockRegisteredTasks;

        /// <summary>
        /// Holds a list of currently running async delegates or delegates that stopped because of unhandled exceptions.
        /// Protected by <see cref="_lockAsyncDelegates"/> lock
        /// </summary>
        private readonly Dictionary<IAsyncDelegate, AsyncTaskInfo> _asyncDelegates;

        /// <summary>
        /// Holds a list of currently registered tasks with their health status.
        /// Protected by <see cref="_lockRegisteredTasks"/> lock
        /// </summary>
        private readonly Dictionary<Task, AsyncTaskInfo> _registeredTasks;

        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly INodeLifetime _nodeLifetime;

        private readonly (string Name, int Width)[] _benchmarkColumnsDefinition = new[]
        {
            (Name: "Name", Width: 80),
            (Name: "Type", Width: 15),
            (Name: "Health", Width: 15)
        };


        public AsyncProvider(ILoggerFactory loggerFactory, INodeLifetime nodeLifetime)
        {
            this._lockAsyncDelegates = new object();
            this._lockRegisteredTasks = new object();

            this._asyncDelegates = new Dictionary<IAsyncDelegate, AsyncTaskInfo>();
            this._registeredTasks = new Dictionary<Task, AsyncTaskInfo>();

            this._loggerFactory = Guard.NotNull(loggerFactory, nameof(loggerFactory));
            this._logger = this._loggerFactory.CreateLogger(this.GetType().FullName);

            this._nodeLifetime = Guard.NotNull(nodeLifetime, nameof(nodeLifetime));
        }

        /// <inheritdoc />
        public IAsyncDelegateDequeuer<T> CreateAndRunAsyncDelegateDequeuer<T>(string friendlyName, Func<T, CancellationToken, Task> @delegate)
        {
            AsyncQueue<T> newDelegate;

            lock (this._lockAsyncDelegates)
            {
                newDelegate = new AsyncQueue<T>(new AsyncQueue<T>.OnEnqueueAsync(@delegate));

                this._asyncDelegates.Add(newDelegate, new AsyncTaskInfo(friendlyName, AsyncTaskInfo.AsyncTaskType.Dequeuer));
            }

            // task will continue with onAsyncDelegateUnhandledException if @delegate had unhandled exceptions
            newDelegate.ConsumerTask.ContinueWith(
                this.OnAsyncDelegateUnhandledException,
                newDelegate,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default
                );

            // task will continue with onAsyncDelegateCompleted if @delegate completed or was canceled
            newDelegate.ConsumerTask.ContinueWith(
                this.OnAsyncDelegateCompleted,
                newDelegate,
                CancellationToken.None,
                TaskContinuationOptions.NotOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default
                );

            return newDelegate;
        }

        /// <inheritdoc />
        public IAsyncLoop CreateAndRunAsyncLoop(string name, Func<CancellationToken, Task> loop, CancellationToken cancellation, TimeSpan? repeatEvery = null, TimeSpan? startAfter = null)
        {
            Guard.NotEmpty(name, nameof(name));
            Guard.NotNull(loop, nameof(loop));
            Guard.NotNull(cancellation, nameof(cancellation));

            // instantiate the loop
            IAsyncLoop loopInstance = new AsyncLoop(name, this._logger, loop);

            Task loopTask;
            lock (this._asyncDelegates)
            {
                this._asyncDelegates.Add(loopInstance, new AsyncTaskInfo(name, AsyncTaskInfo.AsyncTaskType.Loop));
            }

            loopTask = loopInstance.Run(cancellation, repeatEvery ?? TimeSpan.FromMilliseconds(_defaultLoopRepeatInterval), startAfter).RunningTask;

            // task will continue with onAsyncDelegateUnhandledException if @delegate had unhandled exceptions
            loopTask.ContinueWith(
                this.OnAsyncDelegateUnhandledException,
                loopInstance,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default
                );

            // task will continue with onAsyncDelegateCompleted if @delegate completed or was canceled
            loopTask.ContinueWith(
                this.OnAsyncDelegateCompleted,
                loopInstance,
                CancellationToken.None,
                TaskContinuationOptions.NotOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default
                );

            return loopInstance;
        }

        /// <inheritdoc />
        public IAsyncLoop CreateAndRunAsyncLoopUntil(string name, CancellationToken cancellation, Func<bool> condition, Action action, Action<Exception> onException, TimeSpan? repeatEvery = null, TimeSpan? startAfter = null)
        {
            CancellationTokenSource linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            return this.CreateAndRunAsyncLoop(name, token =>
            {
                try
                {
                    // loop until the condition is met, then execute the action and finish.
                    if (condition())
                    {
                        action();

                        linkedTokenSource.Cancel();
                    }
                }
                catch (Exception e)
                {
                    onException(e);
                    linkedTokenSource.Cancel();
                }

                return Task.CompletedTask;
            },
            linkedTokenSource.Token,
            repeatEvery: repeatEvery);
        }

        /// <inheritdoc />
        public IAsyncQueue<T> CreateAsyncQueue<T>()
        {
            return new AsyncQueue<T>();
        }

        /// <inheritdoc />
        public Task RegisterTask(string name, Task taskToRegister)
        {
            Guard.NotEmpty(name, nameof(name));
            Guard.NotNull(taskToRegister, nameof(taskToRegister));

            // instantiate the loop

            lock (this._lockRegisteredTasks)
            {
                this._registeredTasks.Add(taskToRegister, new AsyncTaskInfo(name, AsyncTaskInfo.AsyncTaskType.RegisteredTask));
            }

            // task will continue with OnRegisteredTaskUnhandledException if @delegate had unhandled exceptions
            taskToRegister.ContinueWith(
                this.OnRegisteredTaskUnhandledException,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default
                );

            // task will continue with OnRegisteredTaskCompleted if @delegate completed or was canceled
            taskToRegister.ContinueWith(
                this.OnRegisteredTaskCompleted,
                CancellationToken.None,
                TaskContinuationOptions.NotOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default
                );

            return taskToRegister;
        }

        /// <inheritdoc />
        public bool IsAsyncDelegateDequeuerRunning(IAsyncDelegate asyncDelegate)
        {
            lock (this._lockAsyncDelegates)
            {
                if (this._asyncDelegates.TryGetValue(asyncDelegate, out AsyncTaskInfo delegateInfo))
                {
                    // task in the dictionaries are either running or faulted so we just look for an asyncDelegate with a not faulted status.
                    return delegateInfo.Status != TaskStatus.Faulted;
                }

                return false;
            }
        }

        /// <inheritdoc />
        public bool IsAsyncDelegateDequeuerRunning(string name)
        {
            lock (this._lockAsyncDelegates)
            {
                // task in the dictionaries are either running or faulted so we just look for an IAsyncDelegate with the given name and status not faulted.
                return this._asyncDelegates.Values.Any(@delegate => @delegate.FriendlyName == name && @delegate.Status != TaskStatus.Faulted && @delegate.Type == AsyncTaskInfo.AsyncTaskType.Dequeuer);
            }
        }

        /// <inheritdoc />
        public bool IsAsyncLoopRunning(string name)
        {
            lock (this._lockAsyncDelegates)
            {
                // task in the dictionaries are either running or faulted so we just look for a dequeuer with the given name and status not faulted.
                return this._asyncDelegates.Values.Any(@delegate => @delegate.FriendlyName == name && @delegate.Status != TaskStatus.Faulted && @delegate.Type == AsyncTaskInfo.AsyncTaskType.Loop);
            }
        }

        /// <inheritdoc />
        public bool IsRegisteredTaskRunning(string name)
        {
            lock (this._lockRegisteredTasks)
            {
                // task in the dictionaries are either running or faulted so we just look for a registered task with the given name and status not faulted.
                return this._registeredTasks.Values.Any(@delegate => @delegate.FriendlyName == name && @delegate.Status != TaskStatus.Faulted && @delegate.Type == AsyncTaskInfo.AsyncTaskType.RegisteredTask);
            }
        }

        /// <inheritdoc />
        public string GetStatistics(bool faultyOnly)
        {
            var taskInformations = new List<AsyncTaskInfo>();
            lock (this._lockAsyncDelegates)
            {
                taskInformations.AddRange(this._asyncDelegates.Values);
            }

            lock (this._lockRegisteredTasks)
            {
                taskInformations.AddRange(this._registeredTasks.Values);
            }

            int running = taskInformations.Where(info => info.IsRunning).Count();
            int faulted = taskInformations.Where(info => !info.IsRunning).Count();

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine($"====== Async loops ======   [Running: {running.ToString()}] [Faulted: {faulted.ToString()}]");

            if (faultyOnly && faulted == 0)
                return sb.ToString(); // If there are no faulty tasks and faultOnly is set to true, return just the header.

            var data =
                from info in taskInformations
                orderby info.FriendlyName
                select new
                {
                    Columns = new string[]
                    {
                        info.FriendlyName,
                        (info.Type.ToString()),
                        (info.IsRunning ? "Running" : "Faulted")
                    },
                    Exception = info.Exception?.Message
                };

            foreach (var item in this._benchmarkColumnsDefinition)
            {
                sb.Append(item.Name.PadRight(item.Width));
            }

            sb.AppendLine();
            sb.AppendLine("-".PadRight(this._benchmarkColumnsDefinition.Sum(column => column.Width), '-'));

            foreach (var row in data)
            {
                // skip non faulty rows (Exception is null) if faultyOnly is set.
                if (faultyOnly && row.Exception == null)
                    continue;

                for (int iColumn = 0; iColumn < this._benchmarkColumnsDefinition.Length; iColumn++)
                {
                    sb.Append(row.Columns[iColumn].PadRight(this._benchmarkColumnsDefinition[iColumn].Width));
                }

                // if exception != null means the loop is faulted, so I show the reason in a row under it, a little indented.
                if (row.Exception != null)
                {
                    sb.AppendLine();
                    sb.Append($"      * Fault Reason: {row.Exception}");
                }

                sb.AppendLine();
            }

            sb.AppendLine("-".PadRight(this._benchmarkColumnsDefinition.Sum(column => column.Width), '-'));

            return sb.ToString();
        }

        /// <inheritdoc />
        public List<(string LoopName, TaskStatus Status)> GetAll()
        {
            var taskInformation = new List<AsyncTaskInfo>();

            lock (this._lockAsyncDelegates)
            {
                taskInformation.AddRange(this._asyncDelegates.Values);
            }

            lock (this._lockRegisteredTasks)
            {
                taskInformation.AddRange(this._registeredTasks.Values);
            }

            List<(string, TaskStatus)> runningTasks = taskInformation.Select(a => (a.FriendlyName, a.Status)).OrderBy(a => a.Item1).ToList();

            return runningTasks;
        }

        private void OnRegisteredTaskCompleted(Task task)
        {
            AsyncTaskInfo itemToRemove;
            lock (this._lockRegisteredTasks)
            {
                if (this._registeredTasks.TryGetValue(task, out itemToRemove))
                {
                    this._registeredTasks.Remove(task);
                }
            }

            if (itemToRemove != null)
            {
                this._logger.LogDebug("Registered task '{0}' Removed. Id: {1}.", itemToRemove.FriendlyName, task.Id);
            }
            else
            {
                // Should never happen.
                this._logger.LogError("Cannot find the registered task with Id {0}.", task.Id);
            }
        }

        /// <summary>
        ///  This method is called when a registered Task throws an unhandled exception.
        /// </summary>
        /// <param name="task">The task causing the exception.</param>
        private void OnRegisteredTaskUnhandledException(Task task)
        {
            AsyncTaskInfo delegateInfo;
            lock (this._lockRegisteredTasks)
            {
                if (this._registeredTasks.TryGetValue(task, out delegateInfo))
                {
                    // casted to IAsyncTaskInfoSetter to be able to set properties
                    IAsyncTaskInfoSetter infoSetter = delegateInfo;

                    infoSetter.Exception = task.Exception.GetBaseException();
                    infoSetter.Status = task.Status;
                }
                else
                {
                    // Should never happen.
                    this._logger.LogError("Cannot find the registered task with Id {0}.", task.Id);
                    return;
                }

                this._logger.LogError(task.Exception.GetBaseException(), "Unhandled exception for registered task {0}.", delegateInfo.FriendlyName);
            }
        }

        /// <summary>
        ///  This method is called when a Task running an <see cref="IAsyncDelegate"/> captured an unhandled exception.
        /// </summary>
        /// <param name="task">The delegate task.</param>
        /// <param name="state">The <see cref="IAsyncDelegate"/> that's run by the delegateTask</param>
        /// <remarks>state can be either of type <see cref="IAsyncDelegateDequeuer{T}"/> or <see cref="IAsyncLoop"/></remarks>
        private void OnAsyncDelegateUnhandledException(Task task, object state)
        {
            AsyncTaskInfo delegateInfo;
            lock (this._lockAsyncDelegates)
            {
                if (this._asyncDelegates.TryGetValue((IAsyncDelegate)state, out delegateInfo))
                {
                    // casted to IAsyncTaskInfoSetter to be able to set properties
                    IAsyncTaskInfoSetter infoSetter = delegateInfo;

                    infoSetter.Exception = task.Exception.GetBaseException();
                    infoSetter.Status = task.Status;
                }
                else
                {
                    // Should never happen.
                    this._logger.LogError("Cannot find the AsyncDelegateInfo related to the faulted task with Id {0}.", task.Id);
                    return;
                }

                this._logger.LogError(task.Exception.GetBaseException(), "Unhandled exception for async delegate worker {0}.", delegateInfo.FriendlyName);
            }
        }

        /// <summary>
        ///  This method is called when a Task running an <see cref="IAsyncDelegate"/> completed or was canceled.
        ///  It removes the task information from the internal dictionary.
        /// </summary>
        /// <param name="task">The delegate task.</param>
        /// <param name="state">The <see cref="IAsyncDelegate"/> that's run by the delegateTask</param>
        private void OnAsyncDelegateCompleted(Task task, object state)
        {
            AsyncTaskInfo itemToRemove;
            lock (this._lockAsyncDelegates)
            {
                if (this._asyncDelegates.TryGetValue((IAsyncDelegate)state, out itemToRemove))
                {
                    // When AsyncLoop fails with an uncaughtException, it handle it completing fine.
                    // I want instead to keep its failed status visible on console so I handle this scenario as faulted task.
                    // TODO: discuss about this decision.
                    if (state is AsyncLoop asyncLoop && asyncLoop.UncaughtException != null)
                    {
                        // casted to IAsyncTaskInfoSetter to be able to set properties
                        IAsyncTaskInfoSetter infoSetter = itemToRemove;

                        infoSetter.Exception = asyncLoop.UncaughtException;
                        infoSetter.Status = TaskStatus.Faulted;

                        this._logger.LogError("Async Loop '{0}' completed with an UncaughtException, marking it as faulted. Task Id: {1}.", itemToRemove.FriendlyName, task.Id);
                        return;
                    }
                    else
                    {
                        this._asyncDelegates.Remove((IAsyncDelegate)state);
                    }
                }
            }

            if (itemToRemove != null)
            {
                this._logger.LogDebug("IAsyncDelegate task '{0}' Removed. Id: {1}.", itemToRemove.FriendlyName, task.Id);
            }
            else
            {
                // Should never happen.
                this._logger.LogError("Cannot find the IAsyncDelegate task with Id {0}.", task.Id);
            }
        }
    }
}
