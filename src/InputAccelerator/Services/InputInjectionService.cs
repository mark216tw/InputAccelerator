using System.Runtime.InteropServices;
using InputAccelerator.Core.Models;
using InputAccelerator.Core.Services;
using InputAccelerator.Interop;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel.DataTransfer;

namespace InputAccelerator.Services;

public sealed class InputInjectionService
{
    private const int InputChunkSize = 32;
    private const ushort VirtualKeyReturn = 0x0D;
    private readonly ForegroundWindowTracker _tracker;
    private readonly ILogger<InputInjectionService> _logger;
    private long _lastFailureTimestamp;

    public InputInjectionService(ForegroundWindowTracker tracker, ILogger<InputInjectionService> logger)
    {
        _tracker = tracker;
        _logger = logger;
    }

    public event EventHandler? InjectionFailed;

    public async Task SendAsync(
        string text,
        InputMode mode,
        bool sendEnter,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        try
        {
            if (await FocusTargetAsync(mode.ToString(), cancellationToken).ConfigureAwait(true) == IntPtr.Zero)
            {
                return;
            }

            var sent = mode == InputMode.ClipboardPaste
                ? await SendViaClipboardAsync(text, cancellationToken).ConfigureAwait(true)
                : await SendViaSimulatedInputAsync(text, cancellationToken).ConfigureAwait(true);

            if (sent && sendEnter)
            {
                await Task.Delay(60, cancellationToken).ConfigureAwait(true);
                await SendEnterKeyAsync(cancellationToken).ConfigureAwait(true);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "輸入文字時發生未預期的錯誤。");
            ReportFailure("輸入文字時發生錯誤。");
        }
    }

    public async Task SendHotkeyAsync(string? hotkey, CancellationToken cancellationToken = default)
    {
        if (!HotkeyParser.TryParse(hotkey, out var sequence))
        {
            ReportFailure("按鍵組合無效。");
            return;
        }

        var batches = HotkeyInputComposer.ComposeSequence(sequence);
        if (batches.Count == 0)
        {
            ReportFailure("按鍵組合無效。");
            return;
        }

        try
        {
            var target = await FocusTargetAsync("Hotkey", cancellationToken).ConfigureAwait(true);
            if (target == IntPtr.Zero)
            {
                return;
            }

            await Task.Delay(35, cancellationToken).ConfigureAwait(true);

            var sent = await Task.Run(
                () => SendBatches(batches, target, cancellationToken),
                cancellationToken).ConfigureAwait(true);

            if (!sent)
            {
                ReportFailure("送出按鍵組合失敗，目標程式可能不接受此輸入方式。");
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "送出按鍵組合時發生未預期的錯誤。");
            ReportFailure("送出按鍵組合時發生錯誤。");
        }
    }

    private async Task<IntPtr> FocusTargetAsync(string mode, CancellationToken cancellationToken)
    {
        var target = _tracker.ResolveInjectionTarget();
        if (target == IntPtr.Zero)
        {
            ReportFailure("找不到可輸入的前景視窗。");
            return IntPtr.Zero;
        }

        var foreground = NativeMethods.GetForegroundWindow();
        _logger.LogDebug(
            "準備送出輸入：目標=0x{Target:X}，前景=0x{Foreground:X}，模式={Mode}。",
            target.ToInt64(),
            foreground.ToInt64(),
            mode);

        if (foreground != target)
        {
            NativeMethods.SetForegroundWindow(target);
            await Task.Delay(70, cancellationToken).ConfigureAwait(true);
        }

        foreground = NativeMethods.GetForegroundWindow();
        if (foreground != target)
        {
            _logger.LogWarning(
                "無法還原焦點：目標=0x{Target:X}，前景=0x{Foreground:X}。",
                target.ToInt64(),
                foreground.ToInt64());
            ReportFailure("無法將焦點還原到目標視窗。");
            return IntPtr.Zero;
        }

        return target;
    }

    private async Task<bool> SendViaSimulatedInputAsync(string text, CancellationToken cancellationToken)
    {
        var units = TextInputComposer.Compose(text);
        if (units.Count == 0)
        {
            return false;
        }

        await Task.Delay(35, cancellationToken).ConfigureAwait(true);

        var success = await Task.Run(
            () =>
            {
                var index = 0;
                while (index < units.Count)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (units[index].Kind == TextInputUnitKind.Unicode)
                    {
                        var chunkEnd = index;
                        while (chunkEnd < units.Count && units[chunkEnd].Kind == TextInputUnitKind.Unicode)
                        {
                            chunkEnd++;
                        }

                        while (index < chunkEnd)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            var count = Math.Min(InputChunkSize, chunkEnd - index);
                            if (!SendUnits(units, index, count))
                            {
                                return false;
                            }

                            index += count;
                            Thread.Sleep(1);
                        }

                        continue;
                    }

                    if (!SendUnits(units, index, 1))
                    {
                        return false;
                    }

                    index++;
                    Thread.Sleep(50);
                }

                return true;
            },
            cancellationToken).ConfigureAwait(true);

        if (!success)
        {
            ReportFailure("模擬鍵盤輸入失敗，目標程式可能不接受此輸入方式。");
            return false;
        }

        return true;
    }

    private async Task<bool> SendViaClipboardAsync(string text, CancellationToken cancellationToken)
    {
        try
        {
            var package = new DataPackage();
            package.SetText(TextEscape.Unescape(text));
            Clipboard.SetContent(package);
            Clipboard.Flush();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "無法寫入剪貼簿。");
            ReportFailure("無法寫入剪貼簿。");
            return false;
        }

        await Task.Delay(120, cancellationToken).ConfigureAwait(true);

        var success = await Task.Run(
            () => SendChord(new HotkeyChord(HotkeyModifiers.Control, 0x56)),
            cancellationToken).ConfigureAwait(true);

        if (!success)
        {
            ReportFailure("剪貼簿貼上失敗，目標程式可能不接受此輸入方式。");
            return false;
        }

        return true;
    }

    private async Task SendEnterKeyAsync(CancellationToken cancellationToken)
    {
        var success = await Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var inputs = new NativeMethods.Input[2];
                inputs[0] = CreateVirtualKeyInput(VirtualKeyReturn, keyUp: false);
                inputs[1] = CreateVirtualKeyInput(VirtualKeyReturn, keyUp: true);
                var inserted = NativeMethods.SendInput(
                    (uint)inputs.Length,
                    inputs,
                    Marshal.SizeOf<NativeMethods.Input>());
                return inserted == inputs.Length;
            },
            cancellationToken).ConfigureAwait(true);

        if (!success)
        {
            ReportFailure("送出 Enter 鍵失敗，目標程式可能不接受此輸入方式。");
        }
    }

    private static bool SendUnits(IReadOnlyList<TextInputUnit> units, int startIndex, int count)
    {
        var inputs = new NativeMethods.Input[count * 2];
        var inputIndex = 0;
        for (var index = 0; index < count; index++)
        {
            var unit = units[startIndex + index];
            var isUnicode = unit.Kind == TextInputUnitKind.Unicode;
            inputs[inputIndex++] = new NativeMethods.Input
            {
                Type = NativeMethods.InputKeyboard,
                Data = new NativeMethods.InputUnion
                {
                    Keyboard = new NativeMethods.KeyboardInput
                    {
                        VirtualKey = isUnicode ? (ushort)0 : unit.Value,
                        ScanCode = isUnicode ? unit.Value : (ushort)0,
                        Flags = isUnicode ? NativeMethods.KeyeventfUnicode : 0,
                    },
                },
            };

            inputs[inputIndex++] = new NativeMethods.Input
            {
                Type = NativeMethods.InputKeyboard,
                Data = new NativeMethods.InputUnion
                {
                    Keyboard = new NativeMethods.KeyboardInput
                    {
                        VirtualKey = isUnicode ? (ushort)0 : unit.Value,
                        ScanCode = isUnicode ? unit.Value : (ushort)0,
                        Flags = (isUnicode ? NativeMethods.KeyeventfUnicode : 0) | NativeMethods.KeyeventfKeyUp,
                    },
                },
            };
        }

        var inserted = NativeMethods.SendInput(
            (uint)inputs.Length,
            inputs,
            Marshal.SizeOf<NativeMethods.Input>());
        return inserted == inputs.Length;
    }

    private static bool SendBatches(
        IReadOnlyList<HotkeyInputBatch> batches,
        IntPtr target,
        CancellationToken cancellationToken)
    {
        var completed = false;
        var heldModifiers = new List<HotkeyInputUnit>(4);
        try
        {
            for (var index = 0; index < batches.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (index > 0 && NativeMethods.GetForegroundWindow() != target)
                {
                    return false;
                }

                var batch = batches[index];
                UpdateHeldModifiers(heldModifiers, batch.Units);
                if (!SendHotkeyUnits(batch.Units))
                {
                    return false;
                }

                if (batch.DelayAfterMilliseconds > 0
                    && cancellationToken.WaitHandle.WaitOne(batch.DelayAfterMilliseconds))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            completed = true;
            return true;
        }
        finally
        {
            if (!completed)
            {
                ReleaseHeldModifiers(heldModifiers);
            }
        }
    }

    private static bool SendChord(HotkeyChord chord)
    {
        var units = HotkeyInputComposer.Compose(chord);
        return units.Count > 0 && SendHotkeyUnits(units);
    }

    private static bool SendHotkeyUnits(IReadOnlyList<HotkeyInputUnit> units)
    {
        if (units.Count == 0)
        {
            return false;
        }

        var inputs = new NativeMethods.Input[units.Count];
        for (var index = 0; index < units.Count; index++)
        {
            var unit = units[index];
            inputs[index] = CreateHotkeyInput(unit.VirtualKey, unit.IsKeyUp, unit.IsExtended);
        }

        var inserted = NativeMethods.SendInput(
            (uint)inputs.Length,
            inputs,
            Marshal.SizeOf<NativeMethods.Input>());
        return inserted == inputs.Length;
    }

    private static void UpdateHeldModifiers(
        List<HotkeyInputUnit> heldModifiers,
        IReadOnlyList<HotkeyInputUnit> units)
    {
        foreach (var unit in units)
        {
            if (!HotkeyParser.IsModifierKey(unit.VirtualKey))
            {
                continue;
            }

            if (unit.IsKeyUp)
            {
                heldModifiers.RemoveAll(held => held.VirtualKey == unit.VirtualKey);
            }
            else if (!heldModifiers.Exists(held => held.VirtualKey == unit.VirtualKey))
            {
                heldModifiers.Add(unit);
            }
        }
    }

    private static void ReleaseHeldModifiers(List<HotkeyInputUnit> heldModifiers)
    {
        if (heldModifiers.Count == 0)
        {
            return;
        }

        var inputs = new NativeMethods.Input[heldModifiers.Count];
        for (var index = 0; index < heldModifiers.Count; index++)
        {
            var unit = heldModifiers[heldModifiers.Count - 1 - index];
            inputs[index] = CreateHotkeyInput(unit.VirtualKey, keyUp: true, unit.IsExtended);
        }

        NativeMethods.SendInput(
            (uint)inputs.Length,
            inputs,
            Marshal.SizeOf<NativeMethods.Input>());
        heldModifiers.Clear();
    }

    private static NativeMethods.Input CreateHotkeyInput(ushort virtualKey, bool keyUp, bool isExtended)
    {
        var flags = keyUp ? NativeMethods.KeyeventfKeyUp : 0u;
        if (isExtended)
        {
            flags |= NativeMethods.KeyeventfExtendedKey;
        }

        return new NativeMethods.Input
        {
            Type = NativeMethods.InputKeyboard,
            Data = new NativeMethods.InputUnion
            {
                Keyboard = new NativeMethods.KeyboardInput
                {
                    VirtualKey = virtualKey,
                    Flags = flags,
                },
            },
        };
    }

    private static NativeMethods.Input CreateVirtualKeyInput(ushort virtualKey, bool keyUp)
    {
        return CreateHotkeyInput(virtualKey, keyUp, isExtended: false);
    }

    private void ReportFailure(string reason)
    {
        _logger.LogWarning("{Reason}", reason);

        var now = Environment.TickCount64;
        var previous = Interlocked.Read(ref _lastFailureTimestamp);
        if (now - previous < 5000)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _lastFailureTimestamp, now, previous) != previous)
        {
            return;
        }

        InjectionFailed?.Invoke(this, EventArgs.Empty);
    }
}

