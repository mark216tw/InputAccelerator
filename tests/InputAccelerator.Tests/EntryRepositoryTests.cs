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
