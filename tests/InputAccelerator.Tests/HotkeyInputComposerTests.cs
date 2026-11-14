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

    [Fact]
    public void ComposeSequence_AltHThenW_ReleasesAltAfterFirstStep()
    {
        var sequence = new HotkeySequence(
        [
            new HotkeyChord(HotkeyModifiers.Alt, 0x48),
            new HotkeyChord(HotkeyModifiers.None, 0x57),
        ]);

        var batches = HotkeyInputComposer.ComposeSequence(sequence);

        Assert.Equal(2, batches.Count);
        Assert.Collection(
            batches[0].Units,
            unit => AssertUnit(unit, 0x12, isKeyUp: false, isExtended: false),
            unit => AssertUnit(unit, 0x48, isKeyUp: false, isExtended: false),
            unit => AssertUnit(unit, 0x48, isKeyUp: true, isExtended: false),
            unit => AssertUnit(unit, 0x12, isKeyUp: true, isExtended: false));
        Assert.Equal(HotkeyInputComposer.InterStepDelayMilliseconds, batches[0].DelayAfterMilliseconds);
        Assert.Collection(
            batches[1].Units,
            unit => AssertUnit(unit, 0x57, isKeyUp: false, isExtended: false),
            unit => AssertUnit(unit, 0x57, isKeyUp: true, isExtended: false));
        Assert.Equal(0, batches[1].DelayAfterMilliseconds);
    }

    [Fact]
    public void ComposeSequence_SharedModifier_StaysHeldAcrossSteps()
    {
        var sequence = new HotkeySequence(
        [
            new HotkeyChord(HotkeyModifiers.Alt, 0x48),
            new HotkeyChord(HotkeyModifiers.Alt, 0x57),
        ]);

        var batches = HotkeyInputComposer.ComposeSequence(sequence);

        Assert.Equal(2, batches.Count);
        Assert.Collection(
            batches[0].Units,
            unit => AssertUnit(unit, 0x12, isKeyUp: false, isExtended: false),
            unit => AssertUnit(unit, 0x48, isKeyUp: false, isExtended: false),
            unit => AssertUnit(unit, 0x48, isKeyUp: true, isExtended: false));
        Assert.Collection(
            batches[1].Units,
            unit => AssertUnit(unit, 0x57, isKeyUp: false, isExtended: false),
            unit => AssertUnit(unit, 0x57, isKeyUp: true, isExtended: false),
            unit => AssertUnit(unit, 0x12, isKeyUp: true, isExtended: false));
    }

    [Fact]
    public void ComposeSequence_SwitchingModifiers_ReleasesAndPressesBetweenSteps()
    {
        var sequence = new HotkeySequence(
        [
            new HotkeyChord(HotkeyModifiers.Control, 0x41),
            new HotkeyChord(HotkeyModifiers.Shift, 0x42),
        ]);

        var batches = HotkeyInputComposer.ComposeSequence(sequence);

        Assert.Collection(
            batches[0].Units,
            unit => AssertUnit(unit, 0x11, isKeyUp: false, isExtended: false),
            unit => AssertUnit(unit, 0x41, isKeyUp: false, isExtended: false),
            unit => AssertUnit(unit, 0x41, isKeyUp: true, isExtended: false),
            unit => AssertUnit(unit, 0x11, isKeyUp: true, isExtended: false));
        Assert.Collection(
            batches[1].Units,
            unit => AssertUnit(unit, 0x10, isKeyUp: false, isExtended: false),
            unit => AssertUnit(unit, 0x42, isKeyUp: false, isExtended: false),
            unit => AssertUnit(unit, 0x42, isKeyUp: true, isExtended: false),
            unit => AssertUnit(unit, 0x10, isKeyUp: true, isExtended: false));
    }

    [Fact]
    public void ComposeSequence_AddingModifier_KeepsSharedModifierHeld()
    {
        var sequence = new HotkeySequence(
        [
            new HotkeyChord(HotkeyModifiers.Control, 0x41),
            new HotkeyChord(HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x42),
        ]);

        var batches = HotkeyInputComposer.ComposeSequence(sequence);

        Assert.Collection(
            batches[0].Units,
            unit => AssertUnit(unit, 0x11, isKeyUp: false, isExtended: false),
            unit => AssertUnit(unit, 0x41, isKeyUp: false, isExtended: false),
            unit => AssertUnit(unit, 0x41, isKeyUp: true, isExtended: false));
        Assert.Collection(
            batches[1].Units,
            unit => AssertUnit(unit, 0x10, isKeyUp: false, isExtended: false),
            unit => AssertUnit(unit, 0x42, isKeyUp: false, isExtended: false),
            unit => AssertUnit(unit, 0x42, isKeyUp: true, isExtended: false),
            unit => AssertUnit(unit, 0x10, isKeyUp: true, isExtended: false),
            unit => AssertUnit(unit, 0x11, isKeyUp: true, isExtended: false));
    }

    [Fact]
    public void ComposeSequence_PreservesExtendedKeyFlags()
    {
        var sequence = new HotkeySequence(
        [
            new HotkeyChord(HotkeyModifiers.Alt, 0x48),
            new HotkeyChord(HotkeyModifiers.Control, 0x2E),
        ]);

        var batches = HotkeyInputComposer.ComposeSequence(sequence);

        Assert.Equal(2, batches.Count);
        Assert.Collection(
            batches[0].Units,
            unit => AssertUnit(unit, 0x12, isKeyUp: false, isExtended: false),
            unit => AssertUnit(unit, 0x48, isKeyUp: false, isExtended: false),
            unit => AssertUnit(unit, 0x48, isKeyUp: true, isExtended: false),
            unit => AssertUnit(unit, 0x12, isKeyUp: true, isExtended: false));
        Assert.Collection(
            batches[1].Units,
            unit => AssertUnit(unit, 0x11, isKeyUp: false, isExtended: false),
            unit => AssertUnit(unit, 0x2E, isKeyUp: false, isExtended: true),
            unit => AssertUnit(unit, 0x2E, isKeyUp: true, isExtended: true),
            unit => AssertUnit(unit, 0x11, isKeyUp: true, isExtended: false));
    }

    [Fact]
    public void ComposeSequence_SingleChord_MatchesChordComposition()
    {
        var chord = new HotkeyChord(HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x43);
        var sequence = new HotkeySequence([chord]);

        var batches = HotkeyInputComposer.ComposeSequence(sequence);

        Assert.Single(batches);
        Assert.Equal(HotkeyInputComposer.Compose(chord), batches[0].Units);
        Assert.Equal(0, batches[0].DelayAfterMilliseconds);
    }

    [Fact]
    public void ComposeSequence_EmptySequence_ReturnsEmpty()
    {
        Assert.Empty(HotkeyInputComposer.ComposeSequence(HotkeySequence.Empty));
    }

    [Fact]
    public void ComposeSequence_UnknownVirtualKey_ReturnsEmpty()
    {
        var sequence = new HotkeySequence([new HotkeyChord(HotkeyModifiers.Control, 0xFF)]);

        Assert.Empty(HotkeyInputComposer.ComposeSequence(sequence));
    }

    private static void AssertUnit(HotkeyInputUnit unit, int virtualKey, bool isKeyUp, bool isExtended)
    {
        Assert.Equal((ushort)virtualKey, unit.VirtualKey);
        Assert.Equal(isKeyUp, unit.IsKeyUp);
        Assert.Equal(isExtended, unit.IsExtended);
    }
}
