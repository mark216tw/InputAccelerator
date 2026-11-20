using System.Collections.Concurrent;
using Microsoft.Windows.ApplicationModel.Resources;

namespace InputAccelerator.Helpers;

internal static class Strings
{
    private static readonly Lazy<ResourceLoader> Loader = new(static () => new ResourceLoader());
    private static readonly ConcurrentDictionary<string, string?> Cache = new(StringComparer.Ordinal);

    public static string Get(string key, string fallback = "")
    {
        var value = Cache.GetOrAdd(key, static resourceKey =>
        {
            try
            {
                var loaded = Loader.Value.GetString(resourceKey);
                return string.IsNullOrEmpty(loaded) ? null : loaded;
            }
            catch (Exception)
            {
                return null;
            }
        });

        return value ?? fallback;
    }

    public static string Format(string key, params object[] arguments)
    {
        var format = Get(key);
        return format.Length == 0 ? string.Empty : string.Format(System.Globalization.CultureInfo.CurrentCulture, format, arguments);
    }
}
