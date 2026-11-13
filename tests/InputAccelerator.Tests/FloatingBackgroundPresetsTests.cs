using InputAccelerator.Core.Models;
using Xunit;

namespace InputAccelerator.Tests;

public sealed class FloatingBackgroundPresetsTests
{
    [Fact]
    public void All_ContainsTwelvePresets()
    {
        Assert.Equal(12, FloatingBackgroundPresets.All.Count);
    }

    [Fact]
    public void All_HasSixDarkAndSixLightPresets()
    {
        Assert.Equal(6, FloatingBackgroundPresets.All.Count(preset => preset.IsDark));
        Assert.Equal(6, FloatingBackgroundPresets.All.Count(preset => !preset.IsDark));
    }

    [Fact]
    public void Get_MapsIndexToPreset()
    {
        Assert.Null(FloatingBackgroundPresets.Get(FloatingBackgroundPresets.FollowSystemIndex));
        Assert.Same(FloatingBackgroundPresets.All[0], FloatingBackgroundPresets.Get(1));
        Assert.Same(FloatingBackgroundPresets.All[^1], FloatingBackgroundPresets.Get(12));
        Assert.Null(FloatingBackgroundPresets.Get(13));
        Assert.Null(FloatingBackgroundPresets.Get(-1));
    }

    [Fact]
    public void IsValidIndex_AcceptsOnlySupportedRange()
    {
        Assert.True(FloatingBackgroundPresets.IsValidIndex(0));
        Assert.True(FloatingBackgroundPresets.IsValidIndex(12));
        Assert.False(FloatingBackgroundPresets.IsValidIndex(-1));
        Assert.False(FloatingBackgroundPresets.IsValidIndex(13));
    }
}
