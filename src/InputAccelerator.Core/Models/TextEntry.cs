namespace InputAccelerator.Core.Models;

public sealed class TextEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public EntryKind Kind { get; set; } = EntryKind.Text;

    public string Content { get; set; } = string.Empty;

    public string? Hotkey { get; set; }

    public string? Label { get; set; }
}
