namespace InputAccelerator.Core.Services;

public static class TextEscape
{
    public static string Unescape(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text.Contains('\\')
            ? text.Replace("\\n", "\n", StringComparison.Ordinal)
            : text;
    }
}
