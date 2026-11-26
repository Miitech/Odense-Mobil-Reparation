namespace Repair.Frontend.Services;

// Owned by one UI component. Invoke from its UI thread.
internal sealed class Debouncer(int delayMilliseconds)
{
    private CancellationTokenSource? current;

    public void Cancel()
    {
        CancellationTokenSource? previous = current;
        current = null;
        previous?.Cancel();
    }

    public async Task Run(Func<CancellationToken, Task> action, bool delayed)
    {
        Cancel();
        var source = new CancellationTokenSource();
        current = source;
        try
        {
            if (delayed && delayMilliseconds > 0)
            {
                await Task.Delay(delayMilliseconds, source.Token);
            }

            source.Token.ThrowIfCancellationRequested();
            await action(source.Token);
        }
        catch (OperationCanceledException) when (source.IsCancellationRequested)
        {
        }
        // Retain the latest token source so a subsequent refresh can invalidate
        // UI callbacks that have been queued but have not yet executed.
        // No wait handle or timer is retained after the delay completes.
    }
}
