using System.Collections.Specialized;
using System.Globalization;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using InputAccelerator.Core.Models;
using InputAccelerator.Helpers;
using InputAccelerator.Services;
using Microsoft.Extensions.Logging;

namespace InputAccelerator.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly EntryListViewModel _entryList;
    private readonly SettingsService _settings;
    private readonly ILogger<MainViewModel> _logger;
    private bool _isLoadingSettings;

    [ObservableProperty]
    public partial bool IsOverlayEnabled { get; set; }

    [ObservableProperty]
    public partial bool AutoEnterAfterSelection { get; set; }

    [ObservableProperty]
    public partial TextEntryViewModel? SelectedEntry { get; set; }

    public MainViewModel(
        EntryListViewModel entryList,
        SettingsService settings,
        ILogger<MainViewModel> logger)
    {
        _entryList = entryList;
        _settings = settings;
        _logger = logger;
        _entryList.Entries.CollectionChanged += OnEntriesCollectionChanged;

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

    public event EventHandler<bool>? OverlayVisibilityChanged;

    public EntryListViewModel Entries => _entryList;

    public bool HasSelection => SelectedEntry is not null;

    public bool CanMoveUp => SelectedEntry is not null && _entryList.CanMove(SelectedEntry, -1);

    public bool CanMoveDown => SelectedEntry is not null && _entryList.CanMove(SelectedEntry, 1);

    public string VersionText { get; }

    public void LoadFromSettings(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _isLoadingSettings = true;
        try
        {
            IsOverlayEnabled = settings.IsOverlayEnabled;
            AutoEnterAfterSelection = settings.AutoEnterAfterSelection;
        }
        finally
        {
            _isLoadingSettings = false;
        }
    }

    public TextEntryViewModel AddEntry(string content)
    {
        var entry = _entryList.Add(content);
        SelectedEntry = entry;
        return entry;
    }

    public void UpdateEntry(TextEntryViewModel entry, string content)
    {
        _entryList.UpdateContent(entry, content);
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

    partial void OnIsOverlayEnabledChanged(bool value)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        _settings.Current.IsOverlayEnabled = value;
        PersistSettingsAsync();
        OverlayVisibilityChanged?.Invoke(this, value);
    }

    partial void OnAutoEnterAfterSelectionChanged(bool value)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        _settings.Current.AutoEnterAfterSelection = value;
        PersistSettingsAsync();
    }

    partial void OnSelectedEntryChanged(TextEntryViewModel? value)
    {
        NotifySelectionStateChanged();
    }

    private void OnEntriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        NotifySelectionStateChanged();
    }

    private void NotifySelectionStateChanged()
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(CanMoveUp));
        OnPropertyChanged(nameof(CanMoveDown));
    }

    private async void PersistSettingsAsync()
    {
        try
        {
            await _settings.SaveAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "儲存設定失敗。");
        }
    }
}
