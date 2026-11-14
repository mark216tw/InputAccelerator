using InputAccelerator.Core.Models;
using InputAccelerator.Core.Services;
using Xunit;

namespace InputAccelerator.Tests;

public sealed class EntryListMergerTests
{
    [Fact]
    public void Merge_PreservesExistingThenImportedOrder()
    {
        var existingId = Guid.NewGuid();
        var importedId = Guid.NewGuid();

        var merged = EntryListMerger.Merge(
            [new TextEntry { Id = existingId, Content = "現有" }],
            [new TextEntry { Id = importedId, Content = "匯入" }]);

        Assert.Equal(2, merged.Count);
        Assert.Equal(existingId, merged[0].Id);
        Assert.Equal("現有", merged[0].Content);
        Assert.Equal(importedId, merged[1].Id);
        Assert.Equal("匯入", merged[1].Content);
    }

    [Fact]
    public void Merge_ReassignsDuplicateIds()
    {
        var sharedId = Guid.NewGuid();

        var merged = EntryListMerger.Merge(
            [new TextEntry { Id = sharedId, Content = "現有" }],
            [
                new TextEntry { Id = sharedId, Content = "匯入一" },
                new TextEntry { Id = sharedId, Content = "匯入二" },
            ]);

        Assert.Equal(3, merged.Count);
        Assert.Equal(sharedId, merged[0].Id);
        Assert.NotEqual(sharedId, merged[1].Id);
        Assert.NotEqual(sharedId, merged[2].Id);
        Assert.NotEqual(merged[1].Id, merged[2].Id);
    }

    [Fact]
    public void Merge_ReassignsEmptyIds()
    {
        var merged = EntryListMerger.Merge(
            [],
            [new TextEntry { Id = Guid.Empty, Content = "無 Id" }]);

        var entry = Assert.Single(merged);
        Assert.NotEqual(Guid.Empty, entry.Id);
    }

    [Fact]
    public void Merge_RoundTripsHotkeyAndLabel()
    {
        var merged = EntryListMerger.Merge(
            [],
            [
                new TextEntry
                {
                    Kind = EntryKind.Hotkey,
                    Hotkey = "Alt+H, W",
                    Label = "序列",
                    Content = "應忽略",
                },
            ]);

        var entry = Assert.Single(merged);
        Assert.Equal(EntryKind.Hotkey, entry.Kind);
        Assert.Equal("Alt+H, W", entry.Hotkey);
        Assert.Equal("序列", entry.Label);
    }
}
