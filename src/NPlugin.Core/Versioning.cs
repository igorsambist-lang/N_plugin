using System;

namespace NPlugin.Core;

public static class Versioning
{
    /// <summary>Парсит версию вида "1.2.3" или "v1.2.3.4"; недостающие части = 0.</summary>
    public static bool TryParse(string value, out Version version)
    {
        version = null;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var s = value.Trim().TrimStart('v', 'V');
        if (!Version.TryParse(s, out var v))
            return false;

        version = v;
        return true;
    }

    /// <summary>&gt; 0 если a новее b; 0 если равны; &lt; 0 если a старше b. Невалидные версии считаются самыми старыми.</summary>
    public static int Compare(string a, string b)
    {
        var okA = TryParse(a, out var va);
        var okB = TryParse(b, out var vb);
        if (!okA && !okB) return 0;
        if (!okA) return -1;
        if (!okB) return 1;
        return va.CompareTo(vb);
    }

    /// <summary>Есть ли на сервере версия новее локальной (или локальной ещё нет).</summary>
    public static bool IsNewer(string serverVersion, string localVersion)
    {
        return Compare(serverVersion, localVersion) > 0;
    }
}

/// <summary>Локальный файл версии установленного плагина.</summary>
public static class LocalVersion
{
    private class Dto
    {
        public string Version { get; set; }
    }

    public static string Read(string path)
    {
        try
        {
            if (!System.IO.File.Exists(path))
                return null;
            var dto = Json.Deserialize<Dto>(System.IO.File.ReadAllText(path));
            return dto?.Version;
        }
        catch
        {
            return null;
        }
    }

    public static void Write(string path, string version)
    {
        System.IO.File.WriteAllText(path, Json.Serialize(new Dto { Version = version }));
    }
}
