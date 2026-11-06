using System;
using System.IO;

namespace NPlugin.Core;

/// <summary>
/// Стандартные пути установки плагина и апдейтера (per-user, без прав администратора).
/// </summary>
public static class Paths
{
    public static string BaseDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NPlugin");

    public static string UpdaterDir => Path.Combine(BaseDir, "updater");
    public static string PluginDir => Path.Combine(BaseDir, "plugin");
    public static string LogsDir => Path.Combine(BaseDir, "logs");
    public static string ConfigPath => Path.Combine(BaseDir, "updater.config.json");
    public static string VersionPath => Path.Combine(BaseDir, "version.json");
    public static string PendingFlagPath => Path.Combine(BaseDir, "update.pending");
    public static string UpdaterExePath => Path.Combine(UpdaterDir, "NPlugin.Updater.exe");

    public static string RevitAddinsDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Autodesk", "Revit", "Addins");

    public static string PluginDirFor(string revitVersion) => Path.Combine(PluginDir, revitVersion);

    public static string AddinManifestPath(string revitVersion) =>
        Path.Combine(RevitAddinsDir, revitVersion, "NPlugin.addin");

    public static void EnsureBaseDirs()
    {
        Directory.CreateDirectory(BaseDir);
        Directory.CreateDirectory(UpdaterDir);
        Directory.CreateDirectory(PluginDir);
        Directory.CreateDirectory(LogsDir);
    }
}
