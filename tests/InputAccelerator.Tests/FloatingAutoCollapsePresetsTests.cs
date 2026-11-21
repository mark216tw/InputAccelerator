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
    [InlineData(3, 1)]
    [InlineData(5, 2)]
    [InlineData(10, 3)]
    [InlineData(20, 4)]
    [InlineData(30, 5)]
    [InlineData(60, 6)]
    public void GetIndex_MapsSecondsToIndex(int seconds, int expectedIndex)
    {
        Assert.Equal(expectedIndex, FloatingAutoCollapsePresets.GetIndex(seconds));
        Assert.Equal(seconds, FloatingAutoCollapsePresets.GetSeconds(expectedIndex));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(3, true)]
    [InlineData(60, true)]
    [InlineData(1, false)]
    [InlineData(120, false)]
    [InlineData(999, false)]
    public void IsValidSeconds_ValidatesAllowedValues(int seconds, bool expected)
    {
        Assert.Equal(expected, FloatingAutoCollapsePresets.IsValidSeconds(seconds));
    }
}
