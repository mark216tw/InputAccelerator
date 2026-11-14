namespace InputAccelerator.Core.Models;

public static class FloatingAutoCollapsePresets
{
    public const int OffSeconds = 0;
    public const int DefaultSeconds = OffSeconds;
    public const int DefaultIndex = 0;

    public static IReadOnlyList<int> AllowedSeconds { get; } = [0, 10, 20, 30, 60, 120, 180, 300];

    public static bool IsValidSeconds(int seconds)
    {
        return AllowedSeconds.Contains(seconds);
    }

    public static int GetIndex(int seconds)
    {
        for (var index = 0; index < AllowedSeconds.Count; index++)
        {
            if (AllowedSeconds[index] == seconds)
            {
                return index;
            }
        }

        return DefaultIndex;
    }

    public static int GetSeconds(int index)
    {
        return index >= 0 && index < AllowedSeconds.Count
            ? AllowedSeconds[index]
            : DefaultSeconds;
    }
}
