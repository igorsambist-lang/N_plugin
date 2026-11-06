using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NPlugin.Core;

/// <summary>
/// Шаблон имени файла с токенами {Токен}: {Номер}, {Имя}, {Дата}, {Модель} и т.п.
/// Неизвестные токены заменяются пустой строкой.
/// </summary>
public static class FileNameTemplate
{
    public static string Render(string template, IDictionary<string, string> values)
    {
        if (string.IsNullOrWhiteSpace(template))
            template = "{Имя}";

        var sb = new StringBuilder();
        var i = 0;
        while (i < template.Length)
        {
            var c = template[i];
            if (c == '{')
            {
                var end = template.IndexOf('}', i + 1);
                if (end > i)
                {
                    var token = template.Substring(i + 1, end - i - 1);
                    if (values != null && values.TryGetValue(token, out var v))
                        sb.Append(v);
                    i = end + 1;
                    continue;
                }
            }

            sb.Append(c);
            i++;
        }

        return Sanitize(sb.ToString());
    }

    /// <summary>Убирает символы, запрещённые в именах файлов Windows, обрезает до 180 символов.</summary>
    public static string Sanitize(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "file";

        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            if (c < 32 || System.Array.IndexOf(invalid, c) >= 0)
                sb.Append('_');
            else
                sb.Append(c);
        }

        var result = sb.ToString().Trim(' ', '.', '_');
        if (result.Length > 180)
            result = result.Substring(0, 180).TrimEnd(' ', '.', '_');
        return string.IsNullOrEmpty(result) ? "file" : result;
    }
}
