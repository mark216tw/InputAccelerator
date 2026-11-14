using InputAccelerator.Core.Models;
using InputAccelerator.Core.Services;
using InputAccelerator.Helpers;
using InputAccelerator.Interop;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;

namespace InputAccelerator.Views;

public sealed partial class HotkeyRecorder : UserControl
{
    private static readonly TimeSpan IdleCompletionDelay = TimeSpan.FromMilliseconds(1200);
    private const int VirtualKeyShift = 0x10;
    private const int VirtualKeyControl = 0x11;
    private const int VirtualKeyMenu = 0x12;
    private const int VirtualKeyLeftWindows = 0x5B;
    private const int VirtualKeyRightWindows = 0x5C;

    private readonly List<HotkeyChord> _steps = [];
    private readonly HashSet<int> _downKeys = [];
    private readonly DispatcherQueueTimer _idleTimer;
    private bool _isRecording;

    public HotkeyRecorder()
    {
        InitializeComponent();
        _idleTimer = DispatcherQueue.CreateTimer();
        _idleTimer.Interval = IdleCompletionDelay;
        _idleTimer.IsRepeating = false;
        _idleTimer.Tick += OnIdleTimerTick;
        RecorderButton.AddHandler(
            UIElement.PreviewKeyDownEvent,
            new KeyEventHandler(OnPreviewKeyDown),
            handledEventsToo: true);
        RecorderButton.AddHandler(
            UIElement.PreviewKeyUpEvent,
            new KeyEventHandler(OnPreviewKeyUp),
            handledEventsToo: true);
        UpdateVisualState();

        var name = Strings.Get("HotkeyRecorder.AutomationProperties.Name", "按鍵組合");
        AutomationProperties.SetName(RecorderButton, name);
        ToolTipService.SetToolTip(RecorderButton, name);
    }

    public event EventHandler? HotkeyChanged;

    public string? Hotkey { get; private set; }

    public void SetHotkey(string? hotkey)
    {
        _isRecording = false;
        _idleTimer.Stop();
        _steps.Clear();
        _downKeys.Clear();
        Hotkey = HotkeyParser.TryParse(hotkey, out var sequence)
            ? HotkeyParser.Format(sequence)
            : null;
        UpdateVisualState();
    }

    public void FocusRecorder()
    {
        RecorderButton.Focus(FocusState.Programmatic);
    }

    public void CommitRecording()
    {
        if (_isRecording)
        {
            CompleteRecording();
        }
    }

    private void OnRecorderClick(object sender, RoutedEventArgs e)
    {
        if (_isRecording)
        {
            CompleteRecording();
            return;
        }

        StartRecording();
    }

    private void StartRecording()
    {
        _isRecording = true;
        _idleTimer.Stop();
        _steps.Clear();
        _downKeys.Clear();
        RecorderButton.Focus(FocusState.Programmatic);
        UpdateVisualState();
    }

    private void OnRecorderLostFocus(object sender, RoutedEventArgs e)
    {
        if (!_isRecording)
        {
            return;
        }

        CompleteRecording();
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

        if (e.Key == VirtualKey.Escape && ReadModifiers() == HotkeyModifiers.None)
        {
            CancelRecording();
            e.Handled = true;
            return;
        }

        if (!_downKeys.Add(key))
        {
            e.Handled = true;
            return;
        }

        var modifiers = ReadModifiers();
        if (!HotkeyParser.TryCreate(modifiers, key, out var chord))
        {
            e.Handled = true;
            return;
        }

        if (_steps.Count >= HotkeyParser.MaxSequenceSteps)
        {
            CompleteRecording();
            e.Handled = true;
            return;
        }

        _steps.Add(chord);
        RestartIdleTimer();
        UpdateVisualState();

        if (_steps.Count >= HotkeyParser.MaxSequenceSteps)
        {
            CompleteRecording();
        }

        e.Handled = true;
    }

    private void OnPreviewKeyUp(object sender, KeyRoutedEventArgs e)
    {
        _downKeys.Remove((int)e.Key);
    }

    private void OnIdleTimerTick(DispatcherQueueTimer sender, object args)
    {
        CompleteRecording();
    }

    private void RestartIdleTimer()
    {
        _idleTimer.Stop();
        _idleTimer.Start();
    }

    private void CancelRecording()
    {
        _isRecording = false;
        _idleTimer.Stop();
        _steps.Clear();
        _downKeys.Clear();
        UpdateVisualState();
    }

    private void CompleteRecording()
    {
        if (!_isRecording)
        {
            return;
        }

        _isRecording = false;
        _idleTimer.Stop();
        _downKeys.Clear();

        if (_steps.Count == 0)
        {
            UpdateVisualState();
            return;
        }

        Hotkey = HotkeyParser.Format(new HotkeySequence(_steps));
        _steps.Clear();
        UpdateVisualState();
        HotkeyChanged?.Invoke(this, EventArgs.Empty);
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
            HotkeyText.Text = _steps.Count == 0
                ? Strings.Get("HotkeyRecorder.RecordingText", "請依序按下組合鍵…")
                : $"{HotkeyParser.Format(new HotkeySequence(_steps))}…";
            PlaceholderText.Visibility = Visibility.Collapsed;
            return;
        }

        HotkeyText.Text = Hotkey ?? string.Empty;
        PlaceholderText.Text = Strings.Get("HotkeyRecorder.PlaceholderText", "點擊後依序按下組合鍵");
        PlaceholderText.Visibility = string.IsNullOrEmpty(Hotkey) ? Visibility.Visible : Visibility.Collapsed;
    }
}
