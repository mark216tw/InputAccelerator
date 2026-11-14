namespace InputAccelerator.Core.Models;

public sealed class EntryImportResult
{
    public EntryImportResult(IReadOnlyList<TextEntry> entries, int skippedCount)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (skippedCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(skippedCount));
        }

        Entries = entries;
        SkippedCount = skippedCount;
    }

    public IReadOnlyList<TextEntry> Entries { get; }

    public int SkippedCount { get; }
}
