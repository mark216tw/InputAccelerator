using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using InputAccelerator.Core.Models;
using InputAccelerator.Core.Services;
using Microsoft.Extensions.Logging;

namespace InputAccelerator.ViewModels;

public sealed class EntryListViewModel : ObservableObject
{
    private readonly EntryRepository _repository;
    private readonly ILogger<EntryListViewModel> _logger;
    private readonly SemaphoreSlim _persistenceGate = new(1, 1);
    private bool _suppressPersistence;

    public EntryListViewModel(EntryRepository repository, ILogger<EntryListViewModel> logger)
    {
        _repository = repository;
        _logger = logger;
        Entries.CollectionChanged += OnEntriesChanged;
    }

    public ObservableCollection<TextEntryViewModel> Entries { get; } = [];

    public bool HasEntries => Entries.Count > 0;

    public void Load(IEnumerable<TextEntry> entries)
    {
        _suppressPersistence = true;
        try
        {
            Entries.Clear();
            foreach (var entry in entries)
            {
                Entries.Add(new TextEntryViewModel(entry));
            }
        }
        finally
        {
            _suppressPersistence = false;
        }

        OnPropertyChanged(nameof(HasEntries));
    }

    public TextEntryViewModel Add(EntryKind kind, string content, string? hotkey, string? label)
    {
        var entry = new TextEntryViewModel(Guid.NewGuid(), content)
        {
            Kind = kind,
            Hotkey = hotkey,
            Label = label,
        };
        Entries.Add(entry);
        return entry;
    }

    public void Update(TextEntryViewModel entry, EntryKind kind, string content, string? hotkey, string? label)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.Kind == kind &&
            string.Equals(entry.Content, content, StringComparison.Ordinal) &&
            string.Equals(entry.Hotkey, hotkey, StringComparison.Ordinal) &&
            string.Equals(entry.Label, label, StringComparison.Ordinal))
        {
            return;
        }

        entry.Kind = kind;
        entry.Content = content;
        entry.Hotkey = hotkey;
        entry.Label = label;
        QueuePersistence();
    }

    public void Remove(TextEntryViewModel entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        Entries.Remove(entry);
    }

    public bool CanMove(TextEntryViewModel? entry, int offset)
    {
        if (entry is null)
        {
            return false;
        }

        var currentIndex = Entries.IndexOf(entry);
        var targetIndex = currentIndex + offset;
        return currentIndex >= 0 && targetIndex >= 0 && targetIndex < Entries.Count;
    }

    public void Move(TextEntryViewModel entry, int offset)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (!CanMove(entry, offset))
        {
            return;
        }

        var currentIndex = Entries.IndexOf(entry);
        Entries.Move(currentIndex, currentIndex + offset);
    }

    public Task SaveNowAsync(CancellationToken cancellationToken = default)
    {
        return WriteSnapshotAsync(cancellationToken);
    }

    public async Task<int> ImportAsync(
        EntryImportResult importResult,
        EntryImportMode mode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(importResult);

        var existing = Entries.Select(static entry => entry.ToModel()).ToList();
        var imported = importResult.Entries;
        var merged = mode == EntryImportMode.Replace
            ? imported
            : EntryListMerger.Merge(existing, imported);

        _suppressPersistence = true;
        try
        {
            Entries.Clear();
            foreach (var entry in merged)
            {
                Entries.Add(new TextEntryViewModel(entry));
            }
        }
        finally
        {
            _suppressPersistence = false;
        }

        OnPropertyChanged(nameof(HasEntries));
        await SaveNowAsync(cancellationToken);

        return imported.Count;
    }

    public Task<EntryImportResult> ReadImportFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        return _repository.ImportFromFileAsync(filePath, cancellationToken);
    }

    public Task ExportAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var snapshot = Entries.Select(static entry => entry.ToModel()).ToList();
        return _repository.ExportToFileAsync(filePath, snapshot, cancellationToken);
    }

    private void OnEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasEntries));
        if (!_suppressPersistence)
        {
            QueuePersistence();
        }
    }

    private void QueuePersistence()
    {
        _ = PersistAsync();
    }

    private Task PersistAsync()
    {
        return WriteSnapshotAsync(CancellationToken.None);
    }

    private async Task WriteSnapshotAsync(CancellationToken cancellationToken)
    {
        await _persistenceGate.WaitAsync(cancellationToken).ConfigureAwait(true);
        try
        {
            var snapshot = Entries.Select(static entry => entry.ToModel()).ToList();
            await _repository.SaveAsync(snapshot, cancellationToken).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "儲存項目清單失敗。");
        }
        finally
        {
            _persistenceGate.Release();
        }
    }
}
