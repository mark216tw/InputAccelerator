using InputAccelerator.Core.Models;

namespace InputAccelerator.Core.Services;

public static class EntryListMerger
{
    public static IReadOnlyList<TextEntry> Merge(
        IEnumerable<TextEntry> existing,
        IEnumerable<TextEntry> imported)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(imported);

        var usedIds = new HashSet<Guid>();
        var result = new List<TextEntry>();
        foreach (var entry in existing)
        {
            result.Add(CopyWithUniqueId(entry, usedIds));
        }

        foreach (var entry in imported)
        {
            result.Add(CopyWithUniqueId(entry, usedIds));
        }

        return result;
    }

    private static TextEntry CopyWithUniqueId(TextEntry source, HashSet<Guid> usedIds)
    {
        ArgumentNullException.ThrowIfNull(source);
        var id = source.Id;
        if (id == Guid.Empty || !usedIds.Add(id))
        {
            id = Guid.NewGuid();
            usedIds.Add(id);
        }

        return new TextEntry
        {
            Id = id,
            Kind = source.Kind,
            Content = source.Content,
            Hotkey = source.Hotkey,
            Label = source.Label,
        };
    }
}
