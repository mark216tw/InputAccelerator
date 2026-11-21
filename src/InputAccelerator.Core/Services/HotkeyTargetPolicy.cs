using InputAccelerator.Core.Models;

namespace InputAccelerator.Core.Services;

public static class HotkeyTargetPolicy
{
    public static bool IsSystemWide(HotkeySequence sequence)
    {
        ArgumentNullException.ThrowIfNull(sequence);

        foreach (var step in sequence.Steps)
        {
            if ((step.Modifiers & HotkeyModifiers.Windows) != 0)
            {
                return true;
            }
        }

        return false;
    }
}
