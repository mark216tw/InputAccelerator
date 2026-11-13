namespace InputAccelerator.Core.Models;

public sealed class TextEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Content { get; set; } = string.Empty;
}
