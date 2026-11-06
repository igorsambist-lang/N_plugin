using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using NPlugin.Core;

namespace NPlugin.Updater.Services;

/// <summary>Загрузка manifest.json и пакетов обновления по HTTP.</summary>
public class ManifestClient
{
    private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

    public async System.Threading.Tasks.Task<UpdateManifest> GetManifestAsync(string baseUrl)
    {
        var url = baseUrl.TrimEnd('/') + "/manifest.json";
        var json = await Http.GetStringAsync(url);
        var manifest = Json.Deserialize<UpdateManifest>(json)
                       ?? throw new InvalidDataException("Пустой или невалидный manifest.json");
        if (!Versioning.TryParse(manifest.Version, out _))
            throw new InvalidDataException("В manifest.json невалидная версия: " + manifest.Version);
        if (manifest.Packages == null || manifest.Packages.Count == 0)
            throw new InvalidDataException("В manifest.json нет пакетов.");
        return manifest;
    }

    public async System.Threading.Tasks.Task DownloadAsync(string url, string destination)
    {
        using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        await using var input = await response.Content.ReadAsStreamAsync();
        await using var output = File.Create(destination);
        await input.CopyToAsync(output);
    }

    public static string Sha256File(string path)
    {
        using var sha = SHA256.Create();
        using var stream = File.OpenRead(path);
        var hash = sha.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
