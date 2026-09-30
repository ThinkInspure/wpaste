namespace WPaste.Core.Paths;

public static class AppPaths
{
    public static string DataRoot =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WPaste");

    public static string DatabasePath => Path.Combine(DataRoot, "wpaste.db");

    public static string SettingsPath => Path.Combine(DataRoot, "settings.json");

    public static string ImagesDirectory => Path.Combine(DataRoot, "Images");
}
