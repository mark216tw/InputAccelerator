using InputAccelerator.Core.Models;
using InputAccelerator.Core.Services;
using Xunit;

namespace InputAccelerator.Tests;

public sealed class HotkeyTargetPolicyTests
{
    [Fact]
    public void IsSystemWide_WinR_ReturnsTrue()
    {
        var sequence = new HotkeySequence([new HotkeyChord(HotkeyModifiers.Windows, 0x52)]);

        Assert.True(HotkeyTargetPolicy.IsSystemWide(sequence));
    }

    [Fact]
    public void IsSystemWide_ControlC_ReturnsFalse()
    {
        var sequence = new HotkeySequence([new HotkeyChord(HotkeyModifiers.Control, 0x43)]);

        Assert.False(HotkeyTargetPolicy.IsSystemWide(sequence));
    }

    [Fact]
    public void IsSystemWide_WinInLaterStep_ReturnsTrue()
    {
        var sequence = new HotkeySequence(
        [
            new HotkeyChord(HotkeyModifiers.Control, 0x41),
            new HotkeyChord(HotkeyModifiers.Windows, 0x45),
        ]);

        Assert.True(HotkeyTargetPolicy.IsSystemWide(sequence));
    }

    [Fact]
    public void IsSystemWide_EmptySequence_ReturnsFalse()
    {
        Assert.False(HotkeyTargetPolicy.IsSystemWide(HotkeySequence.Empty));
    }
}
