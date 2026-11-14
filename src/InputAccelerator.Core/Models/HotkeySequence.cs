namespace InputAccelerator.Core.Models;

public sealed class HotkeySequence
{
    public static HotkeySequence Empty { get; } = new(Array.Empty<HotkeyChord>());

    public HotkeySequence(IReadOnlyList<HotkeyChord> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);

        var copy = new HotkeyChord[steps.Count];
        for (var index = 0; index < steps.Count; index++)
        {
            copy[index] = steps[index];
        }

        Steps = copy;
    }

    public IReadOnlyList<HotkeyChord> Steps { get; }

    public bool IsEmpty => Steps.Count == 0;

    public int Count => Steps.Count;
}
