using System.Collections.Specialized;
using System.Globalization;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using InputAccelerator.Core.Models;
using InputAccelerator.Helpers;

namespace InputAccelerator.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly EntryListViewModel _entryList;

    [ObservableProperty]
    public partial TextEntryViewModel? SelectedEntry { get; set; }

    public MainViewModel(EntryListViewModel entryList)
    {
        _entryList = entryList;
        _entryList.Entries.CollectionChanged += OnEntriesCollectionChanged;
        _entryList.BatchUpdateCompleted += OnEntriesBatchUpdateCompleted;

        var assembly = Assembly.GetExecutingAssembly();
        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        var versionText = string.IsNullOrWhiteSpace(informational)
            ? assembly.GetName().Version?.ToString(3) ?? "1.0.0"
            : informational.Split('+')[0];
        var format = Strings.Get("VersionFormat.Text", "版本 {0}");
        VersionText = string.Format(CultureInfo.CurrentCulture, format, versionText);
    }

    public EntryListViewModel Entries => _entryList;

    public bool HasSelection => SelectedEntry is not null;

    public bool CanMoveUp => SelectedEntry is not null && _entryList.CanMove(SelectedEntry, -1);

    public bool CanMoveDown => SelectedEntry is not null && _entryList.CanMove(SelectedEntry, 1);

    public string VersionText { get; }

    public TextEntryViewModel AddEntry(EntryKind kind, string content, string? hotkey, string? label)
    {
        var entry = _entryList.Add(kind, content, hotkey, label);
        SelectedEntry = entry;
        return entry;
    }

    public void UpdateEntry(TextEntryViewModel entry, EntryKind kind, string content, string? hotkey, string? label)
    {
        _entryList.Update(entry, kind, content, hotkey, label);
    }

    public void RemoveEntry(TextEntryViewModel entry)
    {
        var removedIndex = _entryList.Entries.IndexOf(entry);
        _entryList.Remove(entry);

        if (_entryList.Entries.Count == 0)
        {
            SelectedEntry = null;
        }
        else if (removedIndex >= 0)
        {
            SelectedEntry = _entryList.Entries[Math.Min(removedIndex, _entryList.Entries.Count - 1)];
        }
    }

    public void MoveSelected(int offset)
    {
        if (SelectedEntry is null)
        {
            return;
        }

        _entryList.Move(SelectedEntry, offset);
        NotifySelectionStateChanged();
    }

    partial void OnSelectedEntryChanged(TextEntryViewModel? value)
    {
        NotifySelectionStateChanged();
    }

    private void OnEntriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_entryList.IsBatchUpdating)
        {
            return;
        }

        NotifySelectionStateChanged();
    }

    private void OnEntriesBatchUpdateCompleted(object? sender, EventArgs e)
    {
        NotifySelectionStateChanged();
    }

    private void NotifySelectionStateChanged()
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(CanMoveUp));
        OnPropertyChanged(nameof(CanMoveDown));
    }
}
