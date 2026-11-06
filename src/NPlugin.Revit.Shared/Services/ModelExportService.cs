using System;
using System.IO;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NPlugin.Core;
using NPlugin.Infrastructure;
using NPlugin.UI.ViewModels;

namespace NPlugin.Services;

/// <summary>
/// Пакетный экспорт моделей со связями (аналог eTransmit).
/// Публичного API eTransmit в Revit нет, поэтому состав пакета собирается через TransmissionData.
/// </summary>
public class ModelExportService
{
    private readonly RevitActionRunner _runner;

    public ModelExportService(RevitActionRunner runner)
    {
        _runner = runner;
    }

    public void Attach(ModelExportViewModel vm)
    {
        vm.ExportRequested += () => _runner.Run(ui => ExportAll(ui, vm));
    }

    private static void ExportAll(UIApplication ui, ModelExportViewModel vm)
    {
        var settings = vm.CaptureSettings();
        vm.Busy = true;
        try
        {
            foreach (var modelPath in settings.Models)
            {
                var row = vm.FindRow(modelPath);
                if (row == null)
                    continue;

                row.Status = "Обработка...";
                try
                {
                    var summary = ExportModel(modelPath, settings.OutputDir);
                    row.Status = $"Готово: связей {summary.Links}, пропущено {summary.Missing}";
                }
                catch (Exception ex)
                {
                    row.Status = "Ошибка: " + ex.Message;
                }
            }

            vm.Status = "Экспорт завершён.";
        }
        finally
        {
            vm.Busy = false;
        }
    }

    private class Summary
    {
        public int Links;
        public int Missing;
    }

    private static Summary ExportModel(string modelPath, string outputDir)
    {
        if (!File.Exists(modelPath))
            throw new FileNotFoundException("Файл не найден.", modelPath);

        var modelName = Path.GetFileNameWithoutExtension(modelPath);
        var modelDir = Path.Combine(outputDir, FileNameTemplate.Sanitize(modelName));
        Directory.CreateDirectory(modelDir);
        File.Copy(modelPath, Path.Combine(modelDir, Path.GetFileName(modelPath)), true);

        var summary = new Summary();
        var modelPathObject = ModelPathUtils.ConvertUserVisiblePathToModelPath(modelPath);
        var transmissionData = TransmissionData.ReadTransmissionData(modelPathObject);
        if (transmissionData == null)
            return summary;

        foreach (var referenceId in transmissionData.GetAllExternalFileReferenceIds())
        {
            try
            {
                var reference = transmissionData.GetLastSavedReferenceData(referenceId)
                                ?? transmissionData.GetDesiredReferenceData(referenceId);
                if (reference == null)
                {
                    summary.Missing++;
                    continue;
                }

                var linkPath = ModelPathUtils.ConvertModelPathToUserVisiblePath(reference.GetAbsolutePath());
                if (string.IsNullOrEmpty(linkPath) || !File.Exists(linkPath))
                {
                    summary.Missing++;
                    continue;
                }

                var destination = GetDestinationPath(modelPath, modelDir, linkPath);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(linkPath, destination, true);
                summary.Links++;
            }
            catch
            {
                summary.Missing++;
            }
        }

        return summary;
    }

    /// <summary>
    /// Сохраняет относительную структуру папок, если ссылка лежит рядом с моделью (общий корень),
    /// иначе кладёт файл в подпапку _links.
    /// </summary>
    private static string GetDestinationPath(string modelPath, string modelDir, string linkPath)
    {
        try
        {
            var modelUri = new Uri(modelPath);
            var linkUri = new Uri(linkPath);
            var relative = Uri.UnescapeDataString(modelUri.MakeRelativeUri(linkUri).OriginalString);
            if (!relative.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relative))
                return Path.Combine(modelDir, relative.Replace('/', Path.DirectorySeparatorChar));
        }
        catch
        {
            // не смогли вычислить относительный путь — кладём в _links
        }

        return Path.Combine(modelDir, "_links", Path.GetFileName(linkPath));
    }
}
