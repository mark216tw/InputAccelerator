using InputAccelerator.Core.Models;
using InputAccelerator.Core.Services;
using Xunit;

namespace InputAccelerator.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "InputAccelerator.Tests",
        Guid.NewGuid().ToString("N"));

    public SettingsStoreTests()
    {
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTripsSettings()
    {
        var path = Path.Combine(_directory, "settings.json");
        var store = new SettingsStore(path);
        var settings = new AppSettings
        {
            IsOverlayEnabled = false,
            InputMode = InputMode.ClipboardPaste,
            Theme = AppTheme.Dark,
            FloatingBackgroundIndex = 5,
            FloatingWindowPlacement = new WindowPlacement { X = 100, Y = 200, Width = 240, Height = 360 },
        };

        await store.SaveAsync(settings);
        var loaded = await store.LoadAsync();

        Assert.False(loaded.IsOverlayEnabled);
        Assert.Equal(InputMode.ClipboardPaste, loaded.InputMode);
        Assert.Equal(AppTheme.Dark, loaded.Theme);
        Assert.Equal(5, loaded.FloatingBackgroundIndex);
        Assert.NotNull(loaded.FloatingWindowPlacement);
        Assert.Equal(100, loaded.FloatingWindowPlacement!.X);
        Assert.Equal(360, loaded.FloatingWindowPlacement.Height);
        Assert.Contains("\"clipboardPaste\"", await File.ReadAllTextAsync(path), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Load_WhenFileMissing_ReturnsDefaults()
    {
        var store = new SettingsStore(Path.Combine(_directory, "settings.json"));

        var loaded = await store.LoadAsync();

        Assert.True(loaded.IsOverlayEnabled);
        Assert.Equal(InputMode.SimulatedInput, loaded.InputMode);
        Assert.Equal(AppTheme.System, loaded.Theme);
        Assert.Equal(FloatingBackgroundPresets.FollowSystemIndex, loaded.FloatingBackgroundIndex);
        Assert.Null(loaded.FloatingWindowPlacement);
    }

    [Fact]
    public async Task Load_WhenAppearanceInvalid_NormalizesValues()
    {
        var path = Path.Combine(_directory, "settings.json");
        await File.WriteAllTextAsync(
            path,
            """
            {
              "isOverlayEnabled": true,
              "inputMode": "simulatedInput",
              "theme": "unknown",
              "floatingBackgroundIndex": 99
            }
            """);
        var store = new SettingsStore(path);

        var loaded = await store.LoadAsync();

        Assert.Equal(AppTheme.System, loaded.Theme);
        Assert.Equal(FloatingBackgroundPresets.FollowSystemIndex, loaded.FloatingBackgroundIndex);
    }

    [Fact]
    public async Task Load_WhenPlacementSizeInvalid_RemovesPlacement()
    {
        var path = Path.Combine(_directory, "settings.json");
        await File.WriteAllTextAsync(
            path,
            """
            {
              "isOverlayEnabled": true,
              "inputMode": "simulatedInput",
              "floatingWindowPlacement": { "x": 0, "y": 0, "width": 0, "height": 0 }
            }
            """);
        var store = new SettingsStore(path);

        var loaded = await store.LoadAsync();

        Assert.Null(loaded.FloatingWindowPlacement);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
