using InputAccelerator.Core.Models;
using Xunit;

namespace InputAccelerator.Tests;

public sealed class FloatingAutoCollapsePresetsTests
{
    [Fact]
    public void Defaults_DisableAutoCollapse()
    {
        Assert.Equal(0, FloatingAutoCollapsePresets.DefaultSeconds);
        Assert.Equal(0, FloatingAutoCollapsePresets.GetSeconds(FloatingAutoCollapsePresets.DefaultIndex));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(10, 1)]
    [InlineData(20, 2)]
    [InlineData(30, 3)]
    [InlineData(60, 4)]
    [InlineData(120, 5)]
    [InlineData(180, 6)]
    [InlineData(300, 7)]
    public void GetIndex_MapsSecondsToIndex(int seconds, int expectedIndex)
    {
        Assert.Equal(expectedIndex, FloatingAutoCollapsePresets.GetIndex(seconds));
        Assert.Equal(seconds, FloatingAutoCollapsePresets.GetSeconds(expectedIndex));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(60, true)]
    [InlineData(300, true)]
    [InlineData(1, false)]
    [InlineData(999, false)]
    public void IsValidSeconds_ValidatesAllowedValues(int seconds, bool expected)
    {
        Assert.Equal(expected, FloatingAutoCollapsePresets.IsValidSeconds(seconds));
    }
}
