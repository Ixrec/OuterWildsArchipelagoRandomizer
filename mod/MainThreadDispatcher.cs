using System;
using System.Collections.Concurrent;
using OWML.Common;

namespace ArchipelagoRandomizer;

/// <summary>
/// Delegate work from threads to the Unity main thread. Work is executed during APRandomizer.Update().
/// To be used when tasks need to happen on the main thread (e.g. UI updates).
/// </summary>
public static class MainThreadDispatcher
{
    private static readonly ConcurrentQueue<Action> queue = new();

    /// <summary>
    /// Queues an action to run on the Unity main thread during the next APRandomizer.Update().
    /// Safe to call from any thread.
    /// </summary>
    public static void Enqueue(Action action)
    {
        if (action != null)
            queue.Enqueue(action);
    }

    /// <summary>
    /// Runs the queued actions.
    /// Must be called from main thread.
    /// </summary>
    public static void DrainOnMainThread()
    {
        // We only run as many tasks as there were when we entered this method to prevent starvation in case
        // we are spammed with actions.
        int budget = queue.Count;
        while (budget-- > 0 && queue.TryDequeue(out var action))
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                APRandomizer.OWMLModConsole.WriteLine(
                    $"MainThreadDispatcher action threw an exception: {ex.Message}\n{ex.StackTrace}",
                    MessageType.Error);
            }
        }
    }
}
