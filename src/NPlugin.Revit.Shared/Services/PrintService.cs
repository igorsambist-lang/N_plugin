using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NPlugin.Core;
using NPlugin.Infrastructure;
using NPlugin.UI.ViewModels;

namespace NPlugin.Services;

public class PrintService
{
    private readonly RevitActionRunner _runner;

    public PrintService(RevitActionRunner runner)
    {
        _runner = runner;
    }

    public void Attach(BatchPrintViewModel vm)
    {
        vm.RefreshRequested += () => _runner.Run(ui => LoadSheets(ui, vm));
        vm.PrintRequested += settings => _runner.Run(ui => Print(ui, settings, vm));
    }

    private static void LoadSheets(UIApplication ui, BatchPrintViewModel vm)
    {
        var doc = ui.ActiveUIDocument?.Document;
        if (doc == null)
        {
            vm.Status = "Нет активного документа.";
            return;
        }

        var sheets = new FilteredElementCollector(doc)
            .OfClass(typeof(ViewSheet))
            .Cast<ViewSheet>()
            .OrderBy(s => s.SheetNumber, StringComparer.OrdinalIgnoreCase)
            .Select(s => new SheetRow { Id = Ids.GetValue(s.Id), Number = s.SheetNumber, Name = s.Name });
        vm.SetSheets(sheets);
    }

    private static void Print(UIApplication ui, BatchPrintSettings settings, BatchPrintViewModel vm)
    {
        var doc = ui.ActiveUIDocument?.Document;
        var selected = settings.Sheets.Where(s => s.Checked).ToList();
        if (doc == null || selected.Count == 0)
        {
            vm.Status = "Нет документа или не выбраны листы.";
            return;
        }

        if (!Directory.Exists(settings.OutputDir))
        {
            vm.Status = "Папка назначения не существует.";
            return;
        }

        vm.Busy = true;
        try
        {
            var sheets = selected
                .Select(r => doc.GetElement(Ids.Of(r.Id)) as ViewSheet)
                .Where(s => s != null)
                .ToList();

            if (sheets.Count == 0)
            {
                vm.Status = "Выбранные листы не найдены в модели (возможно, список устарел — обновите его).";
                return;
            }

            if (settings.Combine)
            {
                var fileName = FileNameTemplate.Render(settings.FileTemplate, CommonValues(doc));
                Export(doc, settings.Format, settings.OutputDir, fileName, sheets.Select(s => s.Id).ToList());
                vm.ReportProgress(1, 1, $"Готово: {fileName}.{Ext(settings.Format)}");
            }
            else
            {
                var done = 0;
                foreach (var sheet in sheets)
                {
                    var fileName = FileNameTemplate.Render(settings.FileTemplate, SheetValues(sheet, doc));
                    ExportOne(doc, settings.Format, settings.OutputDir, fileName, sheet);
                    done++;
                    vm.ReportProgress(done, sheets.Count, $"Выгружено {done} из {sheets.Count}: {fileName}");
                }

                vm.ReportProgress(sheets.Count, sheets.Count, $"Готово. Выгружено {sheets.Count} листов.");
            }

            if (settings.OpenAfter)
                Process.Start(new ProcessStartInfo { FileName = settings.OutputDir, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            vm.Status = "Ошибка: " + ex.Message;
        }
        finally
        {
            vm.Busy = false;
        }
    }

    private static Dictionary<string, string> CommonValues(Document doc)
    {
        return new Dictionary<string, string>
        {
            ["Модель"] = Path.GetFileNameWithoutExtension(doc.PathName) ?? "model",
            ["Дата"] = DateTime.Now.ToString("yyyy-MM-dd")
        };
    }

    private static Dictionary<string, string> SheetValues(ViewSheet sheet, Document doc)
    {
        var values = CommonValues(doc);
        values["Номер"] = sheet.SheetNumber;
        values["Имя"] = sheet.Name;
        return values;
    }

    private static string Ext(string format)
    {
        return format == "DWG" ? "dwg" : "pdf";
    }

    private static void Export(Document doc, string format, string dir, string name, List<ElementId> ids)
    {
        if (format == "DWG")
            doc.Export(dir, name, ids, new DWGExportOptions());
        else
            doc.Export(dir, ids, new PDFExportOptions { Combine = true, FileName = name });
    }

    private static void ExportOne(Document doc, string format, string dir, string name, ViewSheet sheet)
    {
        var ids = new List<ElementId> { sheet.Id };
        if (format == "DWG")
        {
            doc.Export(dir, name, ids, new DWGExportOptions());
            return;
        }

        // При Combine=false Revit именует файл по имени вида, а не по параметру name.
        // Выгружаем во временную подпапку и переименовываем единственный созданный файл.
        var tmp = Path.Combine(dir, ".npl_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        try
        {
            doc.Export(tmp, ids, new PDFExportOptions());
            var produced = Directory.GetFiles(tmp, "*.pdf").FirstOrDefault();
            if (produced == null)
                throw new InvalidOperationException("Revit не создал PDF-файл.");

            var target = Path.Combine(dir, name + ".pdf");
            if (File.Exists(target))
                File.Delete(target);
            File.Move(produced, target);
        }
        finally
        {
            try { Directory.Delete(tmp, true); }
            catch { /* временная папка останется — не критично */ }
        }
    }
}
