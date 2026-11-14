using InputAccelerator.Core.Models;
using InputAccelerator.Core.Services;
using InputAccelerator.Helpers;
using InputAccelerator.Interop;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace InputAccelerator.Views;

public sealed partial class HotkeyRecorder : UserControl
{
    private const int VirtualKeyShift = 0x10;
    private const int VirtualKeyControl = 0x11;
    private const int VirtualKeyMenu = 0x12;
    private const int VirtualKeyLeftWindows = 0x5B;
    private const int VirtualKeyRightWindows = 0x5C;

    private bool _isRecording;

    public HotkeyRecorder()
    {
        InitializeComponent();
        UpdateVisualState();
        RecorderButton.AddHandler(
            UIElement.PreviewKeyDownEvent,
            new KeyEventHandler(OnPreviewKeyDown),
            handledEventsToo: true);

        var name = Strings.Get("HotkeyRecorder.AutomationProperties.Name", "按鍵組合");
        AutomationProperties.SetName(RecorderButton, name);
        ToolTipService.SetToolTip(RecorderButton, name);
    }

    public event EventHandler? HotkeyChanged;

    public string? Hotkey { get; private set; }

    public void SetHotkey(string? hotkey)
    {
        Hotkey = HotkeyParser.TryParse(hotkey, out var chord)
            ? HotkeyParser.Format(chord)
            : null;
        UpdateVisualState();
    }

    public void FocusRecorder()
    {
        RecorderButton.Focus(FocusState.Programmatic);
    }

    private void OnRecorderClick(object sender, RoutedEventArgs e)
    {
        StartRecording();
    }

    private void StartRecording()
    {
        _isRecording = true;
        RecorderButton.Focus(FocusState.Programmatic);
        UpdateVisualState();
    }

    private void OnRecorderLostFocus(object sender, RoutedEventArgs e)
    {
        if (!_isRecording)
        {
            return;
        }

        _isRecording = false;
        UpdateVisualState();
    }

    private void OnPreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (!_isRecording)
        {
            return;
        }

        var key = (int)e.Key;
        if (key == 0 || HotkeyParser.IsModifierKey(key))
        {
            e.Handled = true;
            return;
        }

        var modifiers = ReadModifiers();
        if (e.Key == VirtualKey.Escape && modifiers == HotkeyModifiers.None)
        {
            _isRecording = false;
            UpdateVisualState();
            e.Handled = true;
            return;
        }

        if (!HotkeyParser.TryCreate(modifiers, key, out var chord))
        {
            e.Handled = true;
            return;
        }

        Hotkey = HotkeyParser.Format(chord);
        _isRecording = false;
        UpdateVisualState();
        HotkeyChanged?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    private static HotkeyModifiers ReadModifiers()
    {
        var modifiers = HotkeyModifiers.None;
        if (IsDown(VirtualKeyControl))
        {
            modifiers |= HotkeyModifiers.Control;
        }

        if (IsDown(VirtualKeyShift))
        {
            modifiers |= HotkeyModifiers.Shift;
        }

        if (IsDown(VirtualKeyMenu))
        {
            modifiers |= HotkeyModifiers.Alt;
        }

        if (IsDown(VirtualKeyLeftWindows) || IsDown(VirtualKeyRightWindows))
        {
            modifiers |= HotkeyModifiers.Windows;
        }

        return modifiers;
    }

    private static bool IsDown(int virtualKey)
    {
        return (NativeMethods.GetAsyncKeyState(virtualKey) & 0x8000) != 0;
    }

    private void UpdateVisualState()
    {
        if (_isRecording)
        {
            HotkeyText.Text = Strings.Get("HotkeyRecorder.RecordingText", "請按下組合鍵…");
            PlaceholderText.Visibility = Visibility.Collapsed;
            return;
        }

        HotkeyText.Text = Hotkey ?? string.Empty;
        PlaceholderText.Text = Strings.Get("HotkeyRecorder.PlaceholderText", "點擊後按下組合鍵");
        PlaceholderText.Visibility = string.IsNullOrEmpty(Hotkey) ? Visibility.Visible : Visibility.Collapsed;
    }
}
