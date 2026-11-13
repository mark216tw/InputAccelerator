using System.Globalization;
using Windows.UI;

namespace InputAccelerator.Helpers;

internal static class ColorUtilities
{
    public static Color Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Color.FromArgb(0, 0, 0, 0);
        }

        var hex = value.Trim().TrimStart('#');
        return hex.Length switch
        {
            6 => Color.FromArgb(
                0xFF,
                ParseByte(hex, 0),
                ParseByte(hex, 2),
                ParseByte(hex, 4)),
            8 => Color.FromArgb(
                ParseByte(hex, 0),
                ParseByte(hex, 2),
                ParseByte(hex, 4),
                ParseByte(hex, 6)),
            _ => Color.FromArgb(0, 0, 0, 0),
        };
    }

    public static Color WithAlpha(Color color, byte alpha)
    {
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    private static byte ParseByte(string value, int start)
    {
        return byte.Parse(
            value.AsSpan(start, 2),
            NumberStyles.HexNumber,
            CultureInfo.InvariantCulture);
    }
}
