using InputAccelerator.Core.Models;
using InputAccelerator.Core.Services;
using InputAccelerator.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace InputAccelerator.Views;

public sealed partial class EditEntryDialog : ContentDialog
{
    public EditEntryDialog(string title, EntryKind kind, string content, string? hotkey, string? label)
    {
        InitializeComponent();
        Title = title;
        PrimaryButtonText = Strings.Get("EditEntryDialog.PrimaryButtonText", "儲存");
        CloseButtonText = Strings.Get("EditEntryDialog.CloseButtonText", "取消");
        KindSelector.SelectedIndex = kind == EntryKind.Hotkey ? 1 : 0;
        ContentBox.Text = content;
        LabelBox.Text = label ?? string.Empty;
        Recorder.SetHotkey(hotkey);
        Recorder.HotkeyChanged += OnHotkeyChanged;
        Loaded += OnLoaded;
        UpdateKindVisibility();
    }

    public EntryKind SelectedKind => KindSelector.SelectedIndex == 1 ? EntryKind.Hotkey : EntryKind.Text;

    public string EntryText => ContentBox.Text;

    public string? EntryHotkey => Recorder.Hotkey;

    public string? EntryLabel => string.IsNullOrWhiteSpace(LabelBox.Text) ? null : LabelBox.Text.Trim();

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (SelectedKind == EntryKind.Hotkey)
        {
            Recorder.FocusRecorder();
            return;
        }

        ContentBox.Focus(FocusState.Programmatic);
        ContentBox.Select(ContentBox.Text.Length, 0);
    }

    private void OnKindSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateKindVisibility();
        ClearValidation();
    }

    private void UpdateKindVisibility()
    {
        var isHotkey = SelectedKind == EntryKind.Hotkey;
        TextPanel.Visibility = isHotkey ? Visibility.Collapsed : Visibility.Visible;
        HotkeyPanel.Visibility = isHotkey ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (SelectedKind == EntryKind.Hotkey)
        {
            if (!HotkeyParser.IsValid(Recorder.Hotkey))
            {
                args.Cancel = true;
                ShowValidation("ValidationHotkey", "請按下要送出的按鍵組合。");
                Recorder.FocusRecorder();
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(ContentBox.Text))
        {
            args.Cancel = true;
            ShowValidation("ValidationEmpty", "請輸入文字內容。");
            ContentBox.Focus(FocusState.Programmatic);
        }
    }

    private void OnContentChanged(object sender, TextChangedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(ContentBox.Text))
        {
            ClearValidation();
        }
    }

    private void OnHotkeyChanged(object? sender, EventArgs e)
    {
        ClearValidation();
    }

    private void ShowValidation(string resourceKey, string fallback)
    {
        ValidationMessage.Text = Strings.Get(resourceKey, fallback);
        ValidationMessage.Visibility = Visibility.Visible;
    }

    private void ClearValidation()
    {
        ValidationMessage.Visibility = Visibility.Collapsed;
    }
}
