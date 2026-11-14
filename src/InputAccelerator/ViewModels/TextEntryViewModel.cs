using CommunityToolkit.Mvvm.ComponentModel;
using InputAccelerator.Core.Models;
using InputAccelerator.Core.Services;
using Microsoft.UI.Xaml;

namespace InputAccelerator.ViewModels;

public sealed partial class TextEntryViewModel : ObservableObject
{
    public TextEntryViewModel(TextEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        Id = entry.Id;
        Kind = Enum.IsDefined(entry.Kind) ? entry.Kind : EntryKind.Text;
        Content = entry.Content;
        Hotkey = entry.Hotkey;
        Label = entry.Label;
    }

    public TextEntryViewModel(Guid id, string content)
    {
        Id = id;
        Kind = EntryKind.Text;
        Content = content;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHotkey))]
    [NotifyPropertyChangedFor(nameof(Preview))]
    [NotifyPropertyChangedFor(nameof(DisplayText))]
    [NotifyPropertyChangedFor(nameof(ToolTipText))]
    [NotifyPropertyChangedFor(nameof(HotkeyVisibility))]
    public partial EntryKind Kind { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Preview))]
    [NotifyPropertyChangedFor(nameof(DisplayText))]
    [NotifyPropertyChangedFor(nameof(ToolTipText))]
    public partial string Content { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Preview))]
    [NotifyPropertyChangedFor(nameof(DisplayText))]
    [NotifyPropertyChangedFor(nameof(ToolTipText))]
    public partial string? Hotkey { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Preview))]
    [NotifyPropertyChangedFor(nameof(DisplayText))]
    [NotifyPropertyChangedFor(nameof(ToolTipText))]
    public partial string? Label { get; set; }

    public Guid Id { get; }

    public bool IsHotkey => Kind == EntryKind.Hotkey;

    public Visibility HotkeyVisibility => IsHotkey ? Visibility.Visible : Visibility.Collapsed;

    public string Preview => Kind == EntryKind.Hotkey
        ? string.IsNullOrWhiteSpace(Label) ? Hotkey ?? string.Empty : Label.Trim()
        : TextPreview.CreateSingleLine(Content);

    public string DisplayText => Kind == EntryKind.Hotkey
        ? string.IsNullOrWhiteSpace(Label) ? Hotkey ?? string.Empty : $"{Label.Trim()}（{Hotkey}）"
        : TextPreview.CreateSingleLine(Content);

    public string ToolTipText => Kind == EntryKind.Hotkey
        ? string.IsNullOrWhiteSpace(Label) ? Hotkey ?? string.Empty : $"{Label.Trim()}（{Hotkey}）"
        : Content;

    public TextEntry ToModel()
    {
        return new TextEntry
        {
            Id = Id,
            Kind = Kind,
            Content = Kind == EntryKind.Text ? Content : string.Empty,
            Hotkey = Kind == EntryKind.Hotkey ? Hotkey : null,
            Label = Kind == EntryKind.Hotkey ? Label : null,
        };
    }
}
