using InputAccelerator.Core.Models;
using InputAccelerator.Core.Services;
using Xunit;

namespace InputAccelerator.Tests;

public sealed class WindowPlacementValidatorTests
{
    private static readonly DisplayAreaInfo Primary =
        new(new PixelRect(0, 0, 1920, 1080), new PixelRect(0, 0, 1920, 1040));

    private static readonly DisplayAreaInfo Secondary =
        new(new PixelRect(1920, 0, 1920, 1080), new PixelRect(1920, 0, 1920, 1040));

    [Fact]
    public void Normalize_WhenPlacementMissing_ReturnsCenteredDefault()
    {
        var result = WindowPlacementValidator.Normalize(
            null,
            [Primary],
            minWidth: 140,
            minHeight: 80,
            defaultWidth: 240,
            defaultHeight: 360);

        Assert.Equal(840, result.X);
        Assert.Equal(340, result.Y);
        Assert.Equal(240, result.Width);
        Assert.Equal(360, result.Height);
    }

    [Fact]
    public void Normalize_WhenPlacementIsOffscreen_ClampsToPrimaryWorkArea()
    {
        var result = WindowPlacementValidator.Normalize(
            new WindowPlacement { X = -5000, Y = 5000, Width = 240, Height = 360 },
            [Primary],
            minWidth: 140,
            minHeight: 80,
            defaultWidth: 240,
            defaultHeight: 360);

        Assert.Equal(0, result.X);
        Assert.Equal(680, result.Y);
        Assert.Equal(240, result.Width);
        Assert.Equal(360, result.Height);
    }

    [Fact]
    public void Normalize_EnforcesMinimumSize()
    {
        var result = WindowPlacementValidator.Normalize(
            new WindowPlacement { X = 100, Y = 100, Width = 10, Height = 10 },
            [Primary],
            minWidth: 140,
            minHeight: 80,
            defaultWidth: 240,
            defaultHeight: 360);

        Assert.Equal(140, result.Width);
        Assert.Equal(80, result.Height);
    }

    [Fact]
    public void Normalize_WhenSizeExceedsWorkArea_ClampsToWorkArea()
    {
        var result = WindowPlacementValidator.Normalize(
            new WindowPlacement { X = 0, Y = 0, Width = 5000, Height = 5000 },
            [Primary],
            minWidth: 140,
            minHeight: 80,
            defaultWidth: 240,
            defaultHeight: 360);

        Assert.Equal(1920, result.Width);
        Assert.Equal(1040, result.Height);
    }

    [Fact]
    public void Normalize_WhenSavedOnSecondaryDisplay_KeepsSecondaryDisplay()
    {
        var result = WindowPlacementValidator.Normalize(
            new WindowPlacement { X = 2000, Y = 100, Width = 240, Height = 360 },
            [Primary, Secondary],
            minWidth: 140,
            minHeight: 80,
            defaultWidth: 240,
            defaultHeight: 360);

        Assert.Equal(2000, result.X);
        Assert.Equal(100, result.Y);
    }

    [Fact]
    public void Normalize_WhenSavedDisplayRemoved_UsesNearestRemainingDisplay()
    {
        var result = WindowPlacementValidator.Normalize(
            new WindowPlacement { X = 4000, Y = 100, Width = 240, Height = 360 },
            [Primary],
            minWidth: 140,
            minHeight: 80,
            defaultWidth: 240,
            defaultHeight: 360);

        Assert.InRange(result.X, 0, 1920 - result.Width);
        Assert.InRange(result.Y, 0, 1040 - result.Height);
    }
}
