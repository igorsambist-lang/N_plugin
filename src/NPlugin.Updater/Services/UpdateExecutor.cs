using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using NPlugin.Core;

namespace NPlugin.Updater.Services;

public class UpdateCheckResult
{
    public bool UpdateAvailable;
    public bool Updated;
    public bool Deferred;
    public string Message = "";
    public string Error;
    public string ServerVersion;
    public string LocalVersion;
}

/// <summary>
/// Проверка обновлений на сервере и установка на ПК:
/// скачивание ZIP-пакетов только для установленных версий Revit, проверка SHA256,
/// бэкап, замена файлов, обновление .addin-манифестов, откат при ошибке.
/// Если Revit запущен — обновление откладывается (флаг update.pending).
/// </summary>
public class UpdateExecutor
{
    private readonly ManifestClient _client = new ManifestClient();

    public event Action<string> LogLine;

    public async Task<UpdateCheckResult> CheckAndInstallAsync(bool install)
    {
        var result = new UpdateCheckResult();
        try
        {
            Paths.EnsureBaseDirs();
            var config = UpdaterConfig.Load(Paths.ConfigPath);
            var local = LocalVersion.Read(Paths.VersionPath);
            result.LocalVersion = local;
            Log($"Локальная версия плагина: {local ?? "не установлена"}");

            var manifest = await _client.GetManifestAsync(config.ServerUrl);
            result.ServerVersion = manifest.Version;
            Log($"Версия на сервере: {manifest.Version} ({manifest.Date})");
            if (!string.IsNullOrEmpty(manifest.ReleaseNotes))
                Log("Что нового: " + manifest.ReleaseNotes);

            if (!Versioning.IsNewer(manifest.Version, local))
            {
                if (File.Exists(Paths.PendingFlagPath))
                    File.Delete(Paths.PendingFlagPath);
                result.Message = "Установлена актуальная версия.";
                return result;
            }

            result.UpdateAvailable = true;
            if (IsRevitRunning())
            {
                File.WriteAllText(Paths.PendingFlagPath, manifest.Version);
                result.Deferred = true;
                result.Message = "Обнаружена новая версия " + manifest.Version +
                                 ", но Revit сейчас запущен. Обновление будет выполнено при следующем запуске.";
                Log(result.Message);
                return result;
            }

            if (!install)
            {
                result.Message = "Доступна новая версия: " + manifest.Version;
                return result;
            }

            await InstallAsync(manifest, config.ServerUrl);
            result.Updated = true;
            result.Message = "Обновлено до версии " + manifest.Version + ".";
            Log(result.Message);
            return result;
        }
        catch (Exception ex)
        {
            Log("Ошибка: " + ex.Message);
            result.Error = ex.Message;
            result.Message = "Ошибка обновления: " + ex.Message;
            return result;
        }
    }

    private async Task InstallAsync(UpdateManifest manifest, string serverUrl)
    {
        var installedRevits = GetInstalledRevitVersions();
        Log("Установленные версии Revit: " + (installedRevits.Count > 0 ? string.Join(", ", installedRevits) : "не найдены"));

        var packages = manifest.Packages
            .Where(p => installedRevits.Contains(p.RevitVersion))
            .ToList();
        if (packages.Count == 0)
        {
            Log("Пакетов для установленных версий Revit нет — ставим все пакеты манифеста.");
            packages = manifest.Packages.ToList();
        }

        var downloadDir = Path.Combine(Paths.BaseDir, "downloads");
        Directory.CreateDirectory(downloadDir);
        var staged = new Dictionary<string, string>(); // revitVersion → распакованная папка

        try
        {
            // Скачивание и проверка
            foreach (var package in packages)
            {
                var zipPath = Path.Combine(downloadDir, package.FileName);
                var url = serverUrl.TrimEnd('/') + "/" + package.FileName;
                Log($"Скачивание {package.FileName} для Revit {package.RevitVersion}...");
                await _client.DownloadAsync(url, zipPath);

                var actualHash = ManifestClient.Sha256File(zipPath);
                if (!string.Equals(actualHash, package.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException(
                        $"SHA256 не совпал для {package.FileName}: ожидалось {package.Sha256}, получено {actualHash}");

                var stageDir = Path.Combine(downloadDir, "stage_" + package.RevitVersion);
                if (Directory.Exists(stageDir))
                    Directory.Delete(stageDir, true);
                ZipFile.ExtractToDirectory(zipPath, stageDir);
                VerifyStagedFiles(package, stageDir);
                staged[package.RevitVersion] = stageDir;
                Log($"Пакет Revit {package.RevitVersion} готов.");
            }

            // Бэкап текущих версий
            var backups = new Dictionary<string, string>();
            foreach (var rev in staged.Keys)
            {
                var pluginDir = Paths.PluginDirFor(rev);
                if (!Directory.Exists(pluginDir))
                    continue;

                var backupDir = pluginDir + ".bak-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                Directory.Move(pluginDir, backupDir);
                backups[rev] = backupDir;
                Log($"Бэкап Revit {rev}: {backupDir}");
            }

            try
            {
                foreach (var pair in staged)
                {
                    var pluginDir = Paths.PluginDirFor(pair.Key);
                    CopyDirectory(pair.Value, pluginDir);
                    WriteAddinManifest(pair.Key);
                    Log($"Файлы Revit {pair.Key} обновлены.");
                }
            }
            catch
            {
                // Откат: восстанавливаем бэкапы
                foreach (var pair in backups)
                {
                    try
                    {
                        var pluginDir = Paths.PluginDirFor(pair.Key);
                        if (Directory.Exists(pluginDir))
                            Directory.Delete(pluginDir, true);
                        Directory.Move(pair.Value, pluginDir);
                    }
                    catch (Exception rollbackEx)
                    {
                        Log($"Ошибка отката Revit {pair.Key}: {rollbackEx.Message}");
                    }
                }

                throw;
            }

            LocalVersion.Write(Paths.VersionPath, manifest.Version);
            if (File.Exists(Paths.PendingFlagPath))
                File.Delete(Paths.PendingFlagPath);

            // Чистим скачанное
            try { Directory.Delete(downloadDir, true); }
            catch { /* не критично */ }
        }
        finally
        {
            foreach (var dir in staged.Values)
            {
                try { Directory.Delete(dir, true); }
                catch { /* не критично */ }
            }
        }
    }

    /// <summary>
    /// Проверяет хэши распакованных файлов: распаковщик ZIP не гарантирует обнаружение
    /// повреждений внутри архива (CRC отдельных записей может не проверяться).
    /// Для манифестов старого формата (без списка файлов) проверка пропускается.
    /// </summary>
    private static void VerifyStagedFiles(UpdatePackage package, string stageDir)
    {
        if (package.Files == null || package.Files.Count == 0)
            return;

        foreach (var file in package.Files)
        {
            var stagedFile = Path.Combine(stageDir, file.Path);
            if (!File.Exists(stagedFile))
                throw new InvalidDataException($"В пакете Revit {package.RevitVersion} отсутствует файл {file.Path}");

            var actualHash = ManifestClient.Sha256File(stagedFile);
            if (!string.Equals(actualHash, file.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException(
                    $"SHA256 файла {file.Path} не совпал: ожидалось {file.Sha256}, получено {actualHash}");
        }
    }

    private static void WriteAddinManifest(string revitVersion)
    {
        var manifestPath = Paths.AddinManifestPath(revitVersion);
        Directory.CreateDirectory(Path.GetDirectoryName(manifestPath));
        var assemblyPath = Path.Combine(Paths.PluginDirFor(revitVersion), "NPlugin.dll");
        File.WriteAllText(manifestPath,
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<RevitAddIns>\n" +
            "  <AddIn Type=\"Application\">\n" +
            "    <Name>N Plugin</Name>\n" +
            $"    <Assembly>{assemblyPath}</Assembly>\n" +
            "    <AddInId>3f9c1e52-b7a4-4d2e-9a6c-1e8f3d5b7a20</AddInId>\n" +
            "    <FullClassName>NPlugin.Application.NPluginApp</FullClassName>\n" +
            "    <VendorId>NPLG</VendorId>\n" +
            "    <VendorDescription>N Plugin</VendorDescription>\n" +
            "  </AddIn>\n" +
            "</RevitAddIns>\n");
    }

    private List<string> GetInstalledRevitVersions()
    {
        var versions = new List<string>();
        try
        {
            var addinsRoot = Paths.RevitAddinsDir;
            if (Directory.Exists(addinsRoot))
            {
                foreach (var dir in Directory.GetDirectories(addinsRoot))
                {
                    var name = Path.GetFileName(dir);
                    if (name.Length == 4 && name.All(char.IsDigit) && !versions.Contains(name))
                        versions.Add(name);
                }
            }
        }
        catch (Exception ex)
        {
            Log("Не удалось определить версии Revit: " + ex.Message);
        }

        return versions;
    }

    private static bool IsRevitRunning()
    {
        try
        {
            return Process.GetProcessesByName("Revit").Any(p => !p.HasExited);
        }
        catch
        {
            return false;
        }
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source))
        {
            var destFile = Path.Combine(destination, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        foreach (var dir in Directory.GetDirectories(source))
        {
            CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)));
        }
    }

    private void Log(string message)
    {
        LogLine?.Invoke(message);
    }
}
