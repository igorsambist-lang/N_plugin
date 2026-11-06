using System;
using System.IO;

namespace NPlugin.Core;

/// <summary>Простой файловый логгер (потокобезопасный).</summary>
public static class Log
{
    private static readonly object Gate = new object();
    private static string _path;

    public static void Init(string filePath)
    {
        _path = filePath;
        try
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
        }
        catch
        {
            // логирование недоступно — не мешаем работе
        }
    }

    public static void Info(string message) => Write("INFO", message);
    public static void Warn(string message) => Write("WARN", message);
    public static void Error(string message) => Write("ERROR", message);

    private static void Write(string level, string message)
    {
        if (_path == null)
            return;

        lock (Gate)
        {
            try
            {
                File.AppendAllText(_path,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}");
            }
            catch
            {
                // ничего не делаем
            }
        }
    }
}
