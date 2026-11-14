using InputAccelerator.Core.Models;
using InputAccelerator.Core.Services;

namespace InputAccelerator.Services;

public sealed class SettingsService
{
    private readonly SettingsStore _store;
    private readonly object _sync = new();
    private readonly SemaphoreSlim _saveGate = new(1, 1);

    public SettingsService(SettingsStore store)
    {
        _store = store;
    }

    public AppSettings Current { get; private set; } = new();

    public event EventHandler? SettingsSaved;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _store.LoadAsync(cancellationToken).ConfigureAwait(true);
        lock (_sync)
        {
            Current = settings;
        }
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await _saveGate.WaitAsync(cancellationToken).ConfigureAwait(true);
        try
        {
            AppSettings snapshot;
            lock (_sync)
            {
                snapshot = Clone(Current);
            }

            await _store.SaveAsync(snapshot, cancellationToken).ConfigureAwait(true);
            SettingsSaved?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _saveGate.Release();
        }
    }

    public void UpdateFloatingWindowPlacement(WindowPlacement placement)
    {
        ArgumentNullException.ThrowIfNull(placement);
        lock (_sync)
        {
            Current.FloatingWindowPlacement = placement.Clone();
        }
    }

    private static AppSettings Clone(AppSettings source)
    {
        return new AppSettings
        {
            IsOverlayEnabled = source.IsOverlayEnabled,
            AutoEnterAfterSelection = source.AutoEnterAfterSelection,
            InputMode = source.InputMode,
            Theme = source.Theme,
            FloatingBackgroundIndex = source.FloatingBackgroundIndex,
            FloatingAutoCollapseSeconds = source.FloatingAutoCollapseSeconds,
            FloatingWindowPlacement = source.FloatingWindowPlacement?.Clone(),
        };
    }
}
