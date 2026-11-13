using InputAccelerator.Core.Services;
using Xunit;

namespace InputAccelerator.Tests;

public sealed class TextEscapeTests
{
    [Fact]
    public void Unescape_ConvertsEscapedNewlineToLineBreak()
    {
        Assert.Equal("第一行\n第二行", TextEscape.Unescape("第一行\\n第二行"));
    }

    [Fact]
    public void Unescape_ConvertsMultipleEscapedNewlines()
    {
        Assert.Equal("a\nb\nc", TextEscape.Unescape("a\\nb\\nc"));
    }

    [Fact]
    public void Unescape_PreservesBackslashWithoutFollowingN()
    {
        Assert.Equal("C:\\temp\\file", TextEscape.Unescape("C:\\temp\\file"));
    }

    [Fact]
    public void Unescape_ConvertsSequenceAnywhereInText()
    {
        Assert.Equal("C:\\temp\name", TextEscape.Unescape("C:\\temp\\name"));
    }

    [Fact]
    public void Unescape_PreservesPlainText()
    {
        Assert.Equal("沒有反斜線", TextEscape.Unescape("沒有反斜線"));
    }

    [Fact]
    public void Unescape_EmptyOrNullReturnsEmpty()
    {
        Assert.Equal(string.Empty, TextEscape.Unescape(string.Empty));
        Assert.Equal(string.Empty, TextEscape.Unescape(null));
    }
}
