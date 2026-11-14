using InputAccelerator.Core.Models;
using InputAccelerator.Core.Services;
using Xunit;

namespace InputAccelerator.Tests;

public sealed class HotkeyParserTests
{
    [Theory]
    [InlineData("Ctrl+C", HotkeyModifiers.Control, 0x43)]
    [InlineData("control+shift+c", HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x43)]
    [InlineData("Alt+F5", HotkeyModifiers.Alt, 0x74)]
    [InlineData("Win+Space", HotkeyModifiers.Windows, 0x20)]
    [InlineData("Ctrl+Alt+Delete", HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x2E)]
    [InlineData("Escape", HotkeyModifiers.None, 0x1B)]
    public void TryParseChord_ReturnsExpectedChord(string text, HotkeyModifiers modifiers, int virtualKey)
    {
        var parsed = HotkeyParser.TryParseChord(text, out var chord);

        Assert.True(parsed);
        Assert.Equal(modifiers, chord.Modifiers);
        Assert.Equal(virtualKey, chord.VirtualKey);
    }

    [Theory]
    [InlineData("shift+control+c", "Ctrl+Shift+C")]
    [InlineData("escape", "Esc")]
    [InlineData("return", "Enter")]
    [InlineData("del", "Delete")]
    [InlineData("windows+f1", "Win+F1")]
    [InlineData("Ctrl+NumpadPlus", "Ctrl+NumpadAdd")]
    public void Format_ReturnsCanonicalText(string text, string expected)
    {
        Assert.True(HotkeyParser.TryParseChord(text, out var chord));

        Assert.Equal(expected, HotkeyParser.Format(chord));
    }

    [Theory]
    [InlineData("Ctrl+Shift+F12")]
    [InlineData("Alt+Left")]
    [InlineData("Ctrl+Backtick")]
    [InlineData("Ctrl+Semicolon")]
    public void ParseAndFormat_RoundTrips(string text)
    {
        Assert.True(HotkeyParser.TryParseChord(text, out var chord));

        Assert.Equal(text, HotkeyParser.Format(chord));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Ctrl")]
    [InlineData("Ctrl+Shift")]
    [InlineData("Ctrl+")]
    [InlineData("Ctrl++C")]
    [InlineData("Ctrl+UnknownKey")]
    [InlineData("Ctrl+C+V")]
    [InlineData("Ctrl+Ctrl+C")]
    public void TryParseChord_RejectsInvalidInput(string? text)
    {
        Assert.False(HotkeyParser.TryParseChord(text, out _));
    }

    [Fact]
    public void TryParse_ParsesAltHThenW()
    {
        Assert.True(HotkeyParser.TryParse("Alt+H, W", out var sequence));

        Assert.Collection(
            sequence.Steps,
            step =>
            {
                Assert.Equal(HotkeyModifiers.Alt, step.Modifiers);
                Assert.Equal(0x48, step.VirtualKey);
            },
            step =>
            {
                Assert.Equal(HotkeyModifiers.None, step.Modifiers);
                Assert.Equal(0x57, step.VirtualKey);
            });
    }

    [Fact]
    public void TryParse_ParsesSharedModifiersAcrossSteps()
    {
        Assert.True(HotkeyParser.TryParse("Alt+H, Alt+W", out var sequence));

        Assert.Collection(
            sequence.Steps,
            step =>
            {
                Assert.Equal(HotkeyModifiers.Alt, step.Modifiers);
                Assert.Equal(0x48, step.VirtualKey);
            },
            step =>
            {
                Assert.Equal(HotkeyModifiers.Alt, step.Modifiers);
                Assert.Equal(0x57, step.VirtualKey);
            });
    }

    [Fact]
    public void TryParse_ParsesSingleChordAsOneStep()
    {
        Assert.True(HotkeyParser.TryParse("Ctrl+C", out var sequence));

        Assert.Single(sequence.Steps);
        Assert.Equal("Ctrl+C", HotkeyParser.Format(sequence));
    }

    [Fact]
    public void TryParse_FormatsSequenceCanonicalText()
    {
        Assert.True(HotkeyParser.TryParse("alt+h, w", out var sequence));

        Assert.Equal("Alt+H, W", HotkeyParser.Format(sequence));
    }

    [Fact]
    public void TryParse_SequenceRoundTrips()
    {
        const string text = "Ctrl+K, Ctrl+C";

        Assert.True(HotkeyParser.TryParse(text, out var sequence));

        Assert.Equal(text, HotkeyParser.Format(sequence));
    }

    [Fact]
    public void TryParse_SupportsCommaKeyName()
    {
        Assert.True(HotkeyParser.TryParse("Ctrl+Comma, W", out var sequence));

        Assert.Equal(0xBC, sequence.Steps[0].VirtualKey);
        Assert.Equal("Ctrl+Comma, W", HotkeyParser.Format(sequence));
    }

    [Fact]
    public void TryParse_AcceptsMaxSequenceSteps()
    {
        Assert.True(HotkeyParser.TryParse("A, B, C, D, E, F, G, H", out var sequence));

        Assert.Equal(HotkeyParser.MaxSequenceSteps, sequence.Count);
    }

    [Theory]
    [InlineData("Alt+H+W")]
    [InlineData("Ctrl+C,")]
    [InlineData(", Ctrl+C")]
    [InlineData("Ctrl+C, ")]
    [InlineData("Ctrl+C, , V")]
    [InlineData("Alt+H, Unknown")]
    [InlineData("A, B, C, D, E, F, G, H, I")]
    public void TryParse_RejectsInvalidSequences(string text)
    {
        Assert.False(HotkeyParser.TryParse(text, out _));
    }

    [Fact]
    public void TryParseChord_RejectsSequenceText()
    {
        Assert.False(HotkeyParser.TryParseChord("Alt+H, W", out _));
    }

    [Fact]
    public void IsValid_AcceptsSequence()
    {
        Assert.True(HotkeyParser.IsValid("Alt+H, W"));
    }

    [Fact]
    public void TryCreate_UsesModifiersAndVirtualKey()
    {
        var created = HotkeyParser.TryCreate(HotkeyModifiers.Control | HotkeyModifiers.Alt, 0x56, out var chord);

        Assert.True(created);
        Assert.Equal("Ctrl+Alt+V", HotkeyParser.Format(chord));
    }

    [Fact]
    public void TryCreate_RejectsUnknownVirtualKey()
    {
        Assert.False(HotkeyParser.TryCreate(HotkeyModifiers.Control, 0xFF, out _));
    }

    [Theory]
    [InlineData(0x2E, true)]
    [InlineData(0x25, true)]
    [InlineData(0x90, true)]
    [InlineData(0x43, false)]
    [InlineData(0x41, false)]
    public void TryGetKeyInfo_ReturnsExtendedFlag(int virtualKey, bool expectedExtended)
    {
        var found = HotkeyParser.TryGetKeyInfo(virtualKey, out _, out var isExtended);

        Assert.True(found);
        Assert.Equal(expectedExtended, isExtended);
    }

    [Theory]
    [InlineData(0x10)]
    [InlineData(0x11)]
    [InlineData(0x12)]
    [InlineData(0x5B)]
    [InlineData(0x5C)]
    [InlineData(0xA2)]
    public void IsModifierKey_ReturnsTrueForModifiers(int virtualKey)
    {
        Assert.True(HotkeyParser.IsModifierKey(virtualKey));
    }

    [Fact]
    public void IsModifierKey_ReturnsFalseForLetter()
    {
        Assert.False(HotkeyParser.IsModifierKey(0x43));
    }
}
