using InputAccelerator.Core.Models;
using InputAccelerator.Core.Services;
using Xunit;

namespace InputAccelerator.Tests;

public sealed class EntryRepositoryTests : IDisposable
{
    private readonly string _directory;
    private readonly string _filePath;

    public EntryRepositoryTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "InputAccelerator.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _filePath = Path.Combine(_directory, "entries.json");
    }

    [Fact]
    public async Task SaveAndLoad_RoundTripsEntriesInOrder()
    {
        var repository = new EntryRepository(_filePath);
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();

        await repository.SaveAsync(
        [
            new TextEntry { Id = firstId, Content = "第一組" },
            new TextEntry { Id = secondId, Content = "第二組\n多行" },
        ]);

        var loaded = await repository.LoadAsync();

        Assert.Equal(2, loaded.Count);
        Assert.Equal(firstId, loaded[0].Id);
        Assert.Equal("第一組", loaded[0].Content);
        Assert.Equal(secondId, loaded[1].Id);
        Assert.Equal("第二組\n多行", loaded[1].Content);
        Assert.False(File.Exists(_filePath + ".tmp"));
    }

    [Fact]
    public async Task Save_SkipsEmptyEntriesAndRepairsMissingIds()
    {
        var repository = new EntryRepository(_filePath);

        await repository.SaveAsync(
        [
            new TextEntry { Id = Guid.Empty, Content = "有效內容" },
            new TextEntry { Id = Guid.NewGuid(), Content = "   " },
        ]);

        var loaded = await repository.LoadAsync();

        Assert.Single(loaded);
        Assert.NotEqual(Guid.Empty, loaded[0].Id);
    }

    [Fact]
    public async Task Load_WhenFileIsCorrupt_ReturnsEmptyAndBacksUpFile()
    {
        await File.WriteAllTextAsync(_filePath, "{ not valid json");
        var repository = new EntryRepository(_filePath);

        var loaded = await repository.LoadAsync();

        Assert.Empty(loaded);
        Assert.False(File.Exists(_filePath));
        Assert.Single(Directory.GetFiles(_directory, "entries.json.bad-*"));
    }

    [Fact]
    public async Task Save_OverwritesExistingFileAtomically()
    {
        var repository = new EntryRepository(_filePath);
        await repository.SaveAsync([new TextEntry { Content = "舊" }]);

        await repository.SaveAsync([new TextEntry { Content = "新" }]);

        var loaded = await repository.LoadAsync();
        Assert.Single(loaded);
        Assert.Equal("新", loaded[0].Content);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTripsHotkeyEntries()
    {
        var repository = new EntryRepository(_filePath);
        var hotkeyId = Guid.NewGuid();

        await repository.SaveAsync(
        [
            new TextEntry
            {
                Id = hotkeyId,
                Kind = EntryKind.Hotkey,
                Hotkey = "control+shift+c",
                Label = "  複製  ",
            },
        ]);

        var loaded = await repository.LoadAsync();

        var entry = Assert.Single(loaded);
        Assert.Equal(hotkeyId, entry.Id);
        Assert.Equal(EntryKind.Hotkey, entry.Kind);
        Assert.Equal("Ctrl+Shift+C", entry.Hotkey);
        Assert.Equal("複製", entry.Label);
        Assert.Equal(string.Empty, entry.Content);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTripsHotkeySequences()
    {
        var repository = new EntryRepository(_filePath);

        await repository.SaveAsync(
        [
            new TextEntry
            {
                Kind = EntryKind.Hotkey,
                Hotkey = "alt+h, w",
                Label = "序列",
            },
        ]);

        var loaded = await repository.LoadAsync();

        var entry = Assert.Single(loaded);
        Assert.Equal(EntryKind.Hotkey, entry.Kind);
        Assert.Equal("Alt+H, W", entry.Hotkey);
        Assert.Equal("序列", entry.Label);
    }

    [Fact]
    public async Task Save_WritesKindAsCamelCaseString()
    {
        var repository = new EntryRepository(_filePath);

        await repository.SaveAsync(
        [
            new TextEntry { Kind = EntryKind.Hotkey, Hotkey = "Ctrl+C", Label = "複製" },
        ]);

        var json = await File.ReadAllTextAsync(_filePath);
        Assert.Contains("\"kind\": \"hotkey\"", json);
        Assert.Contains("\"hotkey\": \"Ctrl+C\"", json);
        Assert.Contains("\"label\": \"複製\"", json);
    }

    [Fact]
    public async Task Load_LegacyEntryWithoutKind_TreatsAsText()
    {
        await File.WriteAllTextAsync(
            _filePath,
            $$"""
            [
              { "id": "{{Guid.NewGuid()}}", "content": "舊版文字" }
            ]
            """);
        var repository = new EntryRepository(_filePath);

        var loaded = await repository.LoadAsync();

        var entry = Assert.Single(loaded);
        Assert.Equal(EntryKind.Text, entry.Kind);
        Assert.Equal("舊版文字", entry.Content);
        Assert.Null(entry.Hotkey);
    }

    [Fact]
    public async Task Load_SkipsHotkeyEntryWithInvalidChord()
    {
        await File.WriteAllTextAsync(
            _filePath,
            $$"""
            [
              { "id": "{{Guid.NewGuid()}}", "kind": "hotkey", "hotkey": "Ctrl+" },
              { "id": "{{Guid.NewGuid()}}", "kind": "hotkey", "hotkey": "Alt+H+W" },
              { "id": "{{Guid.NewGuid()}}", "kind": "text", "content": "有效文字" }
            ]
            """);
        var repository = new EntryRepository(_filePath);

        var loaded = await repository.LoadAsync();

        var entry = Assert.Single(loaded);
        Assert.Equal(EntryKind.Text, entry.Kind);
        Assert.Equal("有效文字", entry.Content);
    }

    [Fact]
    public async Task Save_SkipsInvalidHotkeyAndSkipsTextWithoutContent()
    {
        var repository = new EntryRepository(_filePath);

        await repository.SaveAsync(
        [
            new TextEntry { Kind = EntryKind.Hotkey, Hotkey = "Ctrl" },
            new TextEntry { Kind = EntryKind.Text, Content = "   " },
            new TextEntry { Kind = EntryKind.Hotkey, Hotkey = "Alt+F4", Label = "  關閉  " },
        ]);

        var loaded = await repository.LoadAsync();

        var entry = Assert.Single(loaded);
        Assert.Equal(EntryKind.Hotkey, entry.Kind);
        Assert.Equal("Alt+F4", entry.Hotkey);
        Assert.Equal("關閉", entry.Label);
    }

    [Fact]
    public async Task ExportAndImport_RoundTripsEntries()
    {
        var repository = new EntryRepository(_filePath);
        var exportPath = Path.Combine(_directory, "export.json");
        var textId = Guid.NewGuid();
        var hotkeyId = Guid.NewGuid();

        await repository.ExportToFileAsync(
            exportPath,
            [
                new TextEntry { Id = textId, Content = "匯出文字" },
                new TextEntry
                {
                    Id = hotkeyId,
                    Kind = EntryKind.Hotkey,
                    Hotkey = "control+shift+c",
                    Label = "  複製  ",
                },
            ]);

        var result = await repository.ImportFromFileAsync(exportPath);

        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(2, result.Entries.Count);
        Assert.Equal(textId, result.Entries[0].Id);
        Assert.Equal("匯出文字", result.Entries[0].Content);
        Assert.Equal(hotkeyId, result.Entries[1].Id);
        Assert.Equal("Ctrl+Shift+C", result.Entries[1].Hotkey);
        Assert.Equal("複製", result.Entries[1].Label);
        Assert.False(File.Exists(exportPath + ".tmp"));
    }

    [Fact]
    public async Task Import_WhenJsonIsInvalid_ThrowsAndKeepsSourceFile()
    {
        var repository = new EntryRepository(_filePath);
        var importPath = Path.Combine(_directory, "import.json");
        await File.WriteAllTextAsync(importPath, "{ not valid json");

        await Assert.ThrowsAsync<System.Text.Json.JsonException>(
            () => repository.ImportFromFileAsync(importPath));

        Assert.True(File.Exists(importPath));
        Assert.Empty(Directory.GetFiles(_directory, "import.json.bad-*"));
    }

    [Fact]
    public async Task Import_ReportsSkippedInvalidEntries()
    {
        var repository = new EntryRepository(_filePath);
        var importPath = Path.Combine(_directory, "import.json");
        await File.WriteAllTextAsync(
            importPath,
            $$"""
            [
              { "id": "{{Guid.NewGuid()}}", "kind": "text", "content": "有效" },
              { "id": "{{Guid.NewGuid()}}", "kind": "text", "content": "   " },
              { "id": "{{Guid.NewGuid()}}", "kind": "hotkey", "hotkey": "Ctrl+" }
            ]
            """);

        var result = await repository.ImportFromFileAsync(importPath);

        var entry = Assert.Single(result.Entries);
        Assert.Equal("有效", entry.Content);
        Assert.Equal(2, result.SkippedCount);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
