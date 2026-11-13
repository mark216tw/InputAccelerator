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

    public TextEntryViewModel Add(string content)
    {
        var entry = new TextEntryViewModel(Guid.NewGuid(), content);
        Entries.Add(entry);
        return entry;
    }

    public void UpdateContent(TextEntryViewModel entry, string content)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (string.Equals(entry.Content, content, StringComparison.Ordinal))
        {
            return;
        }

        entry.Content = content;
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
            _logger.LogError(ex, "儲存文字清單失敗。");
        }
        finally
        {
            _persistenceGate.Release();
        }
    }
}
