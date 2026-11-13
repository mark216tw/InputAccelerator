using InputAccelerator.Core.Services;
using Xunit;

namespace InputAccelerator.Tests;

public sealed class TextPreviewTests
{
    [Fact]
    public void CreateSingleLine_UsesFirstNonEmptyLine()
    {
        var result = TextPreview.CreateSingleLine("\r\n  第一行  \r\n第二行");

        Assert.Equal("第一行", result);
    }

    [Fact]
    public void CreateSingleLine_CollapsesWhitespace()
    {
        var result = TextPreview.CreateSingleLine("hello\t\tworld   again");

        Assert.Equal("hello world again", result);
    }

    [Fact]
    public void CreateSingleLine_TruncatesWithEllipsis()
    {
        var result = TextPreview.CreateSingleLine("abcdefghij", maxLength: 5);

        Assert.Equal("abcde…", result);
    }

    [Fact]
    public void CreateSingleLine_EmptyContentReturnsEmpty()
    {
        Assert.Equal(string.Empty, TextPreview.CreateSingleLine(null));
        Assert.Equal(string.Empty, TextPreview.CreateSingleLine("   \r\n\t"));
    }
}
