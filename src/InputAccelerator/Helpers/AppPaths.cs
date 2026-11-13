namespace InputAccelerator.Helpers;

internal static class AppPaths
{
    private static readonly string BaseDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "InputAccelerator");

    public static string DataDirectory => BaseDirectory;

    public static string EntriesFile => Path.Combine(BaseDirectory, "entries.json");

    public static string SettingsFile => Path.Combine(BaseDirectory, "settings.json");

    public static string LogFile => Path.Combine(BaseDirectory, "logs", "app.log");

    public static string AssetsDirectory => Path.Combine(AppContext.BaseDirectory, "Assets");

    public static string IconFile => Path.Combine(AssetsDirectory, "AppIcon.ico");

    public static void EnsureDataDirectory()
    {
        Directory.CreateDirectory(BaseDirectory);
        Directory.CreateDirectory(Path.Combine(BaseDirectory, "logs"));
    }
}
