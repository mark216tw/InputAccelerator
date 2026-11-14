using InputAccelerator.Core.Models;
using InputAccelerator.Core.Services;
using Xunit;

namespace InputAccelerator.Tests;

public sealed class HotkeyInputComposerTests
{
    [Fact]
    public void Compose_ControlC_DownsModifierThenKeyAndReleasesInReverse()
    {
        var units = HotkeyInputComposer.Compose(new HotkeyChord(HotkeyModifiers.Control, 0x43));

        Assert.Collection(
            units,
            unit => AssertUnit(unit, 0x11, isKeyUp: false, isExtended: false),
            unit => AssertUnit(unit, 0x43, isKeyUp: false, isExtended: false),
            unit => AssertUnit(unit, 0x43, isKeyUp: true, isExtended: false),
            unit => AssertUnit(unit, 0x11, isKeyUp: true, isExtended: false));
    }

    [Fact]
    public void Compose_AllModifiers_PressesInOrderAndReleasesReversed()
    {
        var chord = new HotkeyChord(
            HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift | HotkeyModifiers.Windows,
            0x74);

        var units = HotkeyInputComposer.Compose(chord);

        Assert.Collection(
            units,
            unit => AssertUnit(unit, 0x11, isKeyUp: false, isExtended: false),
            unit => AssertUnit(unit, 0x12, isKeyUp: false, isExtended: false),
            unit => AssertUnit(unit, 0x10, isKeyUp: false, isExtended: false),
            unit => AssertUnit(unit, 0x5B, isKeyUp: false, isExtended: true),
            unit => AssertUnit(unit, 0x74, isKeyUp: false, isExtended: false),
            unit => AssertUnit(unit, 0x74, isKeyUp: true, isExtended: false),
            unit => AssertUnit(unit, 0x5B, isKeyUp: true, isExtended: true),
            unit => AssertUnit(unit, 0x10, isKeyUp: true, isExtended: false),
            unit => AssertUnit(unit, 0x12, isKeyUp: true, isExtended: false),
            unit => AssertUnit(unit, 0x11, isKeyUp: true, isExtended: false));
    }

    [Fact]
    public void Compose_ExtendedKey_MarksKeyAndModifierUnits()
    {
        var units = HotkeyInputComposer.Compose(new HotkeyChord(HotkeyModifiers.Control, 0x2E));

        Assert.Collection(
            units,
            unit => AssertUnit(unit, 0x11, isKeyUp: false, isExtended: false),
            unit => AssertUnit(unit, 0x2E, isKeyUp: false, isExtended: true),
            unit => AssertUnit(unit, 0x2E, isKeyUp: true, isExtended: true),
            unit => AssertUnit(unit, 0x11, isKeyUp: true, isExtended: false));
    }

    [Fact]
    public void Compose_WithoutModifiers_ReturnsKeyPressOnly()
    {
        var units = HotkeyInputComposer.Compose(new HotkeyChord(HotkeyModifiers.None, 0x74));

        Assert.Collection(
            units,
            unit => AssertUnit(unit, 0x74, isKeyUp: false, isExtended: false),
            unit => AssertUnit(unit, 0x74, isKeyUp: true, isExtended: false));
    }

    [Fact]
    public void Compose_UnknownVirtualKey_ReturnsEmpty()
    {
        var units = HotkeyInputComposer.Compose(new HotkeyChord(HotkeyModifiers.Control, 0xFF));

        Assert.Empty(units);
    }

    private static void AssertUnit(HotkeyInputUnit unit, int virtualKey, bool isKeyUp, bool isExtended)
    {
        Assert.Equal((ushort)virtualKey, unit.VirtualKey);
        Assert.Equal(isKeyUp, unit.IsKeyUp);
        Assert.Equal(isExtended, unit.IsExtended);
    }
}
