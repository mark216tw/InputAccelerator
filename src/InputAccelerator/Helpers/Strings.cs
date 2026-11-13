using Microsoft.Windows.ApplicationModel.Resources;

namespace InputAccelerator.Helpers;

internal static class Strings
{
    private static readonly ResourceLoader Loader = new();

    public static string Get(string key, string fallback = "")
    {
        try
        {
            var value = Loader.GetString(key);
            return string.IsNullOrEmpty(value) ? fallback : value;
        }
        catch (Exception)
        {
            return fallback;
        }
    }

    public static string Format(string key, params object[] arguments)
    {
        var format = Get(key);
        return format.Length == 0 ? string.Empty : string.Format(System.Globalization.CultureInfo.CurrentCulture, format, arguments);
    }
}
