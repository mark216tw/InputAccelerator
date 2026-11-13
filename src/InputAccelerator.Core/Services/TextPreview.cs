using System.Text.RegularExpressions;

namespace InputAccelerator.Core.Services;

public static partial class TextPreview
{
    public static string CreateSingleLine(string? content, int maxLength = 120)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var normalized = content.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var firstLine = normalized
            .Split('\n')
            .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line))
            ?.Trim() ?? string.Empty;

        if (firstLine.Length == 0)
        {
            return string.Empty;
        }

        firstLine = WhitespaceRegex().Replace(firstLine, " ");
        if (firstLine.Length <= maxLength)
        {
            return firstLine;
        }

        return string.Concat(firstLine.AsSpan(0, maxLength), "…");
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
