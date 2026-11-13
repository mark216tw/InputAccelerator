using InputAccelerator.Core.Services;
using Xunit;

namespace InputAccelerator.Tests;

public sealed class TextInputComposerTests
{
    [Fact]
    public void Compose_MapsTextToUnicodeUnits()
    {
        var units = TextInputComposer.Compose("字A");

        Assert.Equal(2, units.Count);
        Assert.Equal(TextInputUnitKind.Unicode, units[0].Kind);
        Assert.Equal((ushort)'字', units[0].Value);
        Assert.Equal((ushort)'A', units[1].Value);
    }

    [Fact]
    public void Compose_ConvertsCrLfToSingleReturn()
    {
        var units = TextInputComposer.Compose("a\r\nb");

        Assert.Equal(3, units.Count);
        Assert.Equal(new TextInputUnit(TextInputUnitKind.VirtualKey, TextInputComposer.VirtualKeyReturn), units[1]);
    }

    [Fact]
    public void Compose_ConvertsLoneNewlinesAndCarriageReturnsToReturn()
    {
        var units = TextInputComposer.Compose("a\nb\rc");

        Assert.Equal(
            [TextInputUnitKind.Unicode, TextInputUnitKind.VirtualKey, TextInputUnitKind.Unicode, TextInputUnitKind.VirtualKey, TextInputUnitKind.Unicode],
            units.Select(static unit => unit.Kind));
    }

    [Fact]
    public void Compose_ConvertsEscapedNewlineToReturn()
    {
        var units = TextInputComposer.Compose("a\\nb");

        Assert.Equal(3, units.Count);
        Assert.Equal(new TextInputUnit(TextInputUnitKind.VirtualKey, TextInputComposer.VirtualKeyReturn), units[1]);
    }

    [Fact]
    public void Compose_ConvertsTabToTabKey()
    {
        var units = TextInputComposer.Compose("a\tb");

        Assert.Equal(new TextInputUnit(TextInputUnitKind.VirtualKey, TextInputComposer.VirtualKeyTab), units[1]);
    }

    [Fact]
    public void Compose_PreservesSurrogatePairAsTwoUnits()
    {
        const string text = "😀";

        var units = TextInputComposer.Compose(text);

        Assert.Equal(2, units.Count);
        Assert.Equal(text[0], units[0].Value);
        Assert.Equal(text[1], units[1].Value);
    }

    [Fact]
    public void Compose_EmptyTextReturnsEmptyUnits()
    {
        Assert.Empty(TextInputComposer.Compose(string.Empty));
        Assert.Empty(TextInputComposer.Compose(null));
    }
}
