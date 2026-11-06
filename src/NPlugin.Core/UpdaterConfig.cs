using System;
using System.IO;

namespace NPlugin.Core;

/// <summary>Конфигурация десктоп-апдейтера.</summary>
public class UpdaterConfig
{
    /// <summary>Базовый URL сервера обновлений (без слэша в конце).</summary>
    public string ServerUrl { get; set; } = "https://example.com/nplugin";

    public bool AutoCheck { get; set; } = true;

    /// <summary>Интервал автопроверки, минуты.</summary>
    public int CheckIntervalMinutes { get; set; } = 60;

    public static UpdaterConfig Load(string path)
    {
        try
        {
            if (File.Exists(path))
                return Json.Deserialize<UpdaterConfig>(File.ReadAllText(path)) ?? new UpdaterConfig();
        }
        catch (Exception)
        {
            // повреждённый конфиг — пересоздаём
        }

        return new UpdaterConfig();
    }

    public void Save(string path)
    {
        File.WriteAllText(path, Json.Serialize(this));
    }
}
