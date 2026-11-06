using System.Collections.Generic;

namespace NPlugin.Core;

/// <summary>
/// Манифест обновления на сервере: версия плагина и пакеты по версиям Revit.
/// </summary>
public class UpdateManifest
{
    public string Version { get; set; }
    public string Date { get; set; }
    public string MinUpdaterVersion { get; set; } = "1.0.0";
    public string ReleaseNotes { get; set; }
    public List<UpdatePackage> Packages { get; set; } = new List<UpdatePackage>();
}

public class UpdatePackage
{
    /// <summary>Версия Revit, например "2022".</summary>
    public string RevitVersion { get; set; }

    /// <summary>Имя ZIP-файла относительно корня URL сервера.</summary>
    public string FileName { get; set; }

    public string Sha256 { get; set; }
    public long Size { get; set; }

    /// <summary>SHA256 каждого файла внутри архива — проверка целостности после распаковки.</summary>
    public List<UpdatePackageFile> Files { get; set; }
}

public class UpdatePackageFile
{
    /// <summary>Путь файла внутри ZIP (относительно корня архива).</summary>
    public string Path { get; set; }

    public string Sha256 { get; set; }
}
