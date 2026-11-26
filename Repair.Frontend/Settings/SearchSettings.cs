namespace Repair.Frontend.Settings;

public sealed class SearchSettings
{
    public const int DEFAULT_DEBOUNCE_DELAY_MILLISECONDS = 500;

    public int DebounceDelayMilliseconds { get; init; } = DEFAULT_DEBOUNCE_DELAY_MILLISECONDS;
}
