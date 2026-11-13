namespace InputAccelerator.Core.Services;

public enum TextInputUnitKind
{
    Unicode,
    VirtualKey,
}

public readonly record struct TextInputUnit(TextInputUnitKind Kind, ushort Value);

public static class TextInputComposer
{
    public const ushort VirtualKeyReturn = 0x0D;

    public const ushort VirtualKeyTab = 0x09;

    public static IReadOnlyList<TextInputUnit> Compose(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<TextInputUnit>();
        }

        var units = new List<TextInputUnit>(text.Length);
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            switch (character)
            {
                case '\r':
                    units.Add(new TextInputUnit(TextInputUnitKind.VirtualKey, VirtualKeyReturn));
                    if (index + 1 < text.Length && text[index + 1] == '\n')
                    {
                        index++;
                    }

                    break;
                case '\n':
                    units.Add(new TextInputUnit(TextInputUnitKind.VirtualKey, VirtualKeyReturn));
                    break;
                case '\t':
                    units.Add(new TextInputUnit(TextInputUnitKind.VirtualKey, VirtualKeyTab));
                    break;
                default:
                    units.Add(new TextInputUnit(TextInputUnitKind.Unicode, character));
                    break;
            }
        }

        return units;
    }
}
