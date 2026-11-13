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
            await _injectionService.SendAsync(
                item.Entry.Content,
                _settings.Current.InputMode,
                _settings.Current.AutoEnterAfterSelection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "送出文字失敗。");
        }
    }

    private void OnEntryListPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EntryListViewModel.HasEntries))
        {
            OnPropertyChanged(nameof(HasEntries));
        }
    }

    private void OnEntriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildItems();
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
            var item = new FloatingEntryItemViewModel(entry);
            if (_palette is { } palette)
            {
                item.ApplyPalette(palette);
            }

            Items.Add(item);
        }

        OnPropertyChanged(nameof(HasEntries));
    }
}
