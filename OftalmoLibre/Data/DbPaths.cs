namespace OftalmoLibre.Data;

public static class DbPaths
{
    private const string AppDirectoryOverrideVariable = "OFTALMOLIBRE_APPDIR";
    private const string PortableModeVariable = "OFTALMOLIBRE_PORTABLE";
    private const string PortableMarkerFileName = "portable.mode";

    public static bool IsPortableMode =>
        Environment.GetEnvironmentVariable(AppDirectoryOverrideVariable) is not { Length: > 0 } &&
        (IsPortableModeEnabledByVariable() || File.Exists(Path.Combine(AppContext.BaseDirectory, PortableMarkerFileName)));

    public static string AppDirectory =>
        Environment.GetEnvironmentVariable(AppDirectoryOverrideVariable) is { Length: > 0 } customDirectory
            ? customDirectory
            : IsPortableMode
                ? AppContext.BaseDirectory
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OftalmoLibre");

    public static string DatabasePath => Path.Combine(AppDirectory, "oftalmolibre.db");

    public static string BackupDirectory => Path.Combine(AppDirectory, "Backups");

    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(AppDirectory);
        Directory.CreateDirectory(BackupDirectory);
    }

    private static bool IsPortableModeEnabledByVariable()
    {
        return Environment.GetEnvironmentVariable(PortableModeVariable) is { Length: > 0 } rawValue &&
               rawValue.Trim().ToLowerInvariant() is "1" or "true" or "yes" or "si";
    }
}
