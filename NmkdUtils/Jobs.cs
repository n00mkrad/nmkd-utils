using System.Collections.Concurrent;
using CT = System.Threading.CancellationToken;

namespace NmkdUtils;
public class Jobs
{
    public const int MaxJobs = 128;

    private static readonly ConcurrentDictionary<Task, byte> _running = new();
    private static readonly object _throttleLock = new();
    private static volatile TaskCompletionSource<object?> _allDoneTcs = NewAllDoneTcs();
    private static int _count;

    public static bool AllCompleted => Volatile.Read(ref _count) == 0;
    public static int RunningCount => Volatile.Read(ref _count);
    public static Task[] RunningTasksSnapshot => _running.Keys.ToArray(); // Snapshot of currently running tasks

    /// <summary> Track an existing task. It's removed automatically on completion. </summary>
    public static Task Add(Task task)
    {
        if (!_running.TryAdd(task, 0))
            return task;

        Interlocked.Increment(ref _count);

        // Ensure new waits don't get a completed TCS
        var tcs = _allDoneTcs;
        if (tcs.Task.IsCompleted)
        {
            while (tcs.Task.IsCompleted)
            {
                var fresh = NewAllDoneTcs();
                var original = Interlocked.CompareExchange(ref _allDoneTcs, fresh, tcs);
                if (ReferenceEquals(original, tcs)) break;
                tcs = original;
            }
        }

        task.ContinueWith(static t => OnTaskCompleted(t), CT.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        return task;
    }

    /// <summary> Start and track a task produced by <paramref name="work"/>. </summary>
    public static Task Run(Func<CT, Task> work, CT ct = default)
        => Add(Task.Run(() => work(ct), ct));

    /// <inheritdoc cref="Run(Func{CT, Task}, CT)"/>
    public static Task Run(Func<Task> work)
        => Add(Task.Run(work));

    /// <summary> Blocks until all tracked tasks finish. </summary>
    public static void WaitAll(CT cancellationToken = default)
        => WaitAllAsync(cancellationToken).GetAwaiter().GetResult();

    /// <summary> Asynchronously waits until all tracked tasks finish. </summary>
    public static Task WaitAllAsync(CT cancellationToken = default)
    {
        var waitTask = _allDoneTcs.Task;
        if (!waitTask.IsCompleted && cancellationToken.CanBeCanceled)
            return WaitWithCancellationAsync(waitTask, cancellationToken);
        return waitTask;
    }

    /// <summary> 
    /// Fire-and-forget: schedules <paramref name="work"/> and returns immediately.
    /// If the global task limit is hit, blocks until below the limit.
    /// </summary>
    public static void Fire(Action work)
    {
        lock (_throttleLock)
        {
            while (RunningCount >= MaxJobs)
                Monitor.Wait(_throttleLock);

            Add(Task.Run(work)); // Schedule and start tracking before releasing the lock to avoid oversubscription.
        }
    }

    /// <summary> 
    /// Fire-and-forget: schedules <paramref name="work"/> and returns immediately.
    /// If the global task limit is hit, blocks until below the limit.
    /// </summary>
    public static void Fire(Func<Task> work)
    {
        lock (_throttleLock)
        {
            while (RunningCount >= MaxJobs)
                Monitor.Wait(_throttleLock);

            Add(Task.Run(work)); // Schedule and start tracking before releasing the lock to avoid oversubscription.
        }
    }

    private static TaskCompletionSource<object?> NewAllDoneTcs()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static async Task WaitWithCancellationAsync(Task waitTask, CT ct)
    {
        var completed = await Task.WhenAny(waitTask, Task.Delay(Timeout.Infinite, ct)).ConfigureAwait(false);
        if (completed != waitTask)
            throw new OperationCanceledException(ct);
        await waitTask.ConfigureAwait(false);
    }

    private static void OnTaskCompleted(Task t)
    {
        _running.TryRemove(t, out _);

        if (t.IsFaulted) _ = t.Exception; // observe to avoid UnobservedTaskException

        var remaining = Interlocked.Decrement(ref _count);
        if (remaining == 0)
        {
            _allDoneTcs.TrySetResult(null);
        }

        // Wake any FireAndForget callers waiting on throttle.
        lock (_throttleLock)
        {
            Monitor.PulseAll(_throttleLock);
        }
    }
}
