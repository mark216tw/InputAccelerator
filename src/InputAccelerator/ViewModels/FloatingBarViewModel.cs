using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using InputAccelerator.Services;
using Microsoft.Extensions.Logging;

namespace InputAccelerator.ViewModels;

public sealed class FloatingBarViewModel : ObservableObject
{
    private readonly EntryListViewModel _entryList;
    private readonly InputInjectionService _injectionService;
    private readonly SettingsService _settings;
    private readonly ILogger<FloatingBarViewModel> _logger;
    private FloatingPalette? _palette;

    public FloatingBarViewModel(
        EntryListViewModel entryList,
        InputInjectionService injectionService,
        SettingsService settings,
        ILogger<FloatingBarViewModel> logger)
    {
        _entryList = entryList;
        _injectionService = injectionService;
        _settings = settings;
        _logger = logger;
        _entryList.PropertyChanged += OnEntryListPropertyChanged;
        _entryList.Entries.CollectionChanged += OnEntriesCollectionChanged;
        _entryList.BatchUpdateCompleted += OnEntriesBatchUpdateCompleted;
        RebuildItems();
    }

    public ObservableCollection<FloatingEntryItemViewModel> Items { get; } = [];

    public bool HasEntries => Items.Count > 0;

    public void ApplyPalette(FloatingPalette palette)
    {
        ArgumentNullException.ThrowIfNull(palette);
        _palette = palette;
        foreach (var item in Items)
        {
            item.ApplyPalette(palette);
        }
    }

    public async Task SendAsync(FloatingEntryItemViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item);
        try
        {
            if (item.Entry.IsHotkey)
            {
                await _injectionService.SendHotkeyAsync(item.Entry.Hotkey);
                return;
            }

            await _injectionService.SendAsync(
                item.Entry.Content,
                _settings.Current.InputMode,
                _settings.Current.AutoEnterAfterSelection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "送出內容失敗。");
        }
    }

    private void OnEntryListPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EntryListViewModel.HasEntries))
        {
            OnPropertyChanged(nameof(HasEntries));
        }
    }

    private void OnEntriesBatchUpdateCompleted(object? sender, EventArgs e)
    {
        RebuildItems();
    }

    private void OnEntriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_entryList.IsBatchUpdating)
        {
            return;
        }

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems is not null && e.NewStartingIndex >= 0:
                var insertIndex = e.NewStartingIndex;
                foreach (TextEntryViewModel entry in e.NewItems)
                {
                    var item = CreateItem(entry);
                    Items.Insert(insertIndex++, item);
                }

                OnPropertyChanged(nameof(HasEntries));
                return;

            case NotifyCollectionChangedAction.Remove when e.OldItems is not null && e.OldStartingIndex >= 0:
                for (var i = 0; i < e.OldItems.Count && e.OldStartingIndex < Items.Count; i++)
                {
                    Items[e.OldStartingIndex].Dispose();
                    Items.RemoveAt(e.OldStartingIndex);
                }

                OnPropertyChanged(nameof(HasEntries));
                return;

            case NotifyCollectionChangedAction.Move when e.OldStartingIndex >= 0 && e.NewStartingIndex >= 0 && e.OldItems?.Count == 1:
                Items.Move(e.OldStartingIndex, e.NewStartingIndex);
                return;

            default:
                RebuildItems();
                return;
        }
    }

    private FloatingEntryItemViewModel CreateItem(TextEntryViewModel entry)
    {
        var item = new FloatingEntryItemViewModel(entry);
        if (_palette is { } palette)
        {
            item.ApplyPalette(palette);
        }

        return item;
    }

    private void RebuildItems()
    {
        foreach (var item in Items)
        {
            item.Dispose();
        }

        Items.Clear();
        foreach (var entry in _entryList.Entries)
        {
            Items.Add(CreateItem(entry));
        }

        OnPropertyChanged(nameof(HasEntries));
    }
}
