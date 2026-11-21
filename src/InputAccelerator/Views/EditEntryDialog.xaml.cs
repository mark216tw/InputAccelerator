using InputAccelerator.Core.Models;
using InputAccelerator.Core.Services;
using InputAccelerator.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace InputAccelerator.Views;

public sealed partial class EditEntryDialog : ContentDialog
{
    private bool _isSyncingHotkey;

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
        _isSyncingHotkey = true;
        HotkeyTextBox.Text = Recorder.Hotkey ?? string.Empty;
        _isSyncingHotkey = false;
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
            Recorder.CommitRecording();

            var text = HotkeyTextBox.Text.Trim();
            if (text.Length > 0)
            {
                if (!HotkeyParser.TryParse(text, out var sequence))
                {
                    args.Cancel = true;
                    ShowValidation("ValidationHotkeyFormat", "按鍵組合格式無效，例如 Win+R 或 Ctrl+K, Ctrl+C。");
                    HotkeyTextBox.Focus(FocusState.Programmatic);
                    return;
                }

                Recorder.SetHotkey(HotkeyParser.Format(sequence));
            }

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
        _isSyncingHotkey = true;
        HotkeyTextBox.Text = Recorder.Hotkey ?? string.Empty;
        HotkeyPresetSelector.SelectedIndex = -1;
        _isSyncingHotkey = false;
        ClearValidation();
    }

    private void OnHotkeyTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isSyncingHotkey)
        {
            return;
        }

        if (HotkeyPresetSelector.SelectedIndex >= 0)
        {
            _isSyncingHotkey = true;
            HotkeyPresetSelector.SelectedIndex = -1;
            _isSyncingHotkey = false;
        }

        var text = HotkeyTextBox.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            Recorder.SetHotkey(null);
        }
        else if (HotkeyParser.TryParse(text, out var sequence))
        {
            Recorder.SetHotkey(HotkeyParser.Format(sequence));
        }

        ClearValidation();
    }

    private void OnHotkeyPresetSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncingHotkey)
        {
            return;
        }

        if (HotkeyPresetSelector.SelectedItem is not ComboBoxItem { Tag: string preset })
        {
            return;
        }

        _isSyncingHotkey = true;
        HotkeyTextBox.Text = preset;
        _isSyncingHotkey = false;
        Recorder.SetHotkey(preset);
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
