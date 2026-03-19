using System;
using System.Threading;
using System.Threading.Tasks;

namespace NetErp.Helpers
{
    /// <summary>
    /// Provides debounce functionality for async actions.
    /// Each call to RunAsync cancels the previous pending execution
    /// and starts a new delay. The action only executes after the
    /// specified delay elapses without any new calls.
    /// </summary>
    public class DebouncedAction(int delayMs = 500)
    {
        private readonly int _delayMs = delayMs;
        private CancellationTokenSource? _cts;

        public async Task RunAsync(Func<Task> action)
        {
            await (_cts?.CancelAsync() ?? Task.CompletedTask);
            _cts = new CancellationTokenSource();
            try
            {
                await Task.Delay(_delayMs, _cts.Token);
                await action();
            }
            catch (TaskCanceledException) { }
        }
    }
}
