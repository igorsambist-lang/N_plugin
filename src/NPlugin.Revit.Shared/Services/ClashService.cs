using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ClosedXML.Excel;
using NPlugin.Core;
using NPlugin.Infrastructure;
using NPlugin.UI.ViewModels;

namespace NPlugin.Services;

public class ClashService
{
    private readonly RevitActionRunner _runner;

    public ClashService(RevitActionRunner runner)
    {
        _runner = runner;
    }

    public void Attach(ClashViewModel vm)
    {
        vm.SetCategories(GetPresetCategories());
        vm.RunRequested += settings => _runner.Run(ui => Run(ui, settings, vm));
        vm.SelectRequested += row => _runner.Run(ui => Select(ui, row));
        vm.ShowViewRequested += row => _runner.Run(ui => ShowView(ui, row));
        vm.ExportExcelRequested += rows => ExportExcel(vm, rows);
    }

    private static IEnumerable<CategoryItem> GetPresetCategories()
    {
        return new[]
        {
            new CategoryItem { Id = (int)BuiltInCategory.OST_DuctCurves, Name = "Воздуховоды" },
            new CategoryItem { Id = (int)BuiltInCategory.OST_PipeCurves, Name = "Трубы" },
            new CategoryItem { Id = (int)BuiltInCategory.OST_CableTray, Name = "Кабельные лотки" },
            new CategoryItem { Id = (int)BuiltInCategory.OST_Conduit, Name = "Кабельные трубы (conduit)" },
            new CategoryItem { Id = (int)BuiltInCategory.OST_StructuralFraming, Name = "Несущий каркас" },
            new CategoryItem { Id = (int)BuiltInCategory.OST_StructuralColumns, Name = "Несущие колонны" },
            new CategoryItem { Id = (int)BuiltInCategory.OST_Floors, Name = "Перекрытия" },
            new CategoryItem { Id = (int)BuiltInCategory.OST_Walls, Name = "Стены" },
            new CategoryItem { Id = (int)BuiltInCategory.OST_Ceilings, Name = "Потолки" }
        };
    }

    private static void Run(UIApplication ui, ClashSettings settings, ClashViewModel vm)
    {
        var doc = ui.ActiveUIDocument?.Document;
        if (doc == null)
        {
            vm.Status = "Нет активного документа.";
            return;
        }

        var categoryA = (BuiltInCategory)settings.LeftCategoryId;
        var categoryB = (BuiltInCategory)settings.RightCategoryId;
        var elementsA = Collect(doc, categoryA);
        var elementsB = Collect(doc, categoryB);
        if (elementsA.Count == 0 || elementsB.Count == 0)
        {
            vm.Status = "Элементы выбранных категорий не найдены.";
            return;
        }

        vm.Busy = true;
        try
        {
            var rows = new List<ClashRow>();
            var done = 0;
            foreach (var elementA in elementsA)
            {
                done++;
                if (done % 5 == 0)
                {
                    vm.ReportProgress(done, elementsA.Count, $"Проверка {done} из {elementsA.Count}...");
                    if (vm.CancelRequested)
                        break;
                }

                ElementIntersectsElementFilter filter;
                try
                {
                    filter = new ElementIntersectsElementFilter(elementA);
                }
                catch
                {
                    continue;
                }

                foreach (var elementB in elementsB)
                {
                    if (elementA.Id == elementB.Id)
                        continue;
                    if (!MepUtils.BoundingBoxesIntersect(elementA, elementB))
                        continue;

                    try
                    {
                        if (!filter.PassesFilter(elementB))
                            continue;
                    }
                    catch
                    {
                        continue;
                    }

                    rows.Add(new ClashRow
                    {
                        AId = Ids.GetValue(elementA.Id),
                        BId = Ids.GetValue(elementB.Id),
                        AName = MepUtils.ElementName(elementA),
                        BName = MepUtils.ElementName(elementB),
                        ASystem = MepUtils.SystemName(elementA),
                        BSystem = MepUtils.SystemName(elementB),
                        Level = MepUtils.LevelName(doc, elementA)
                    });
                }
            }

            vm.SetClashes(rows);
            if (vm.CancelRequested)
                vm.Status = $"Отменено. Найдено пересечений: {rows.Count}.";
        }
        finally
        {
            vm.Busy = false;
        }
    }

    private static List<Element> Collect(Document doc, BuiltInCategory category)
    {
        return new FilteredElementCollector(doc)
            .OfCategory(category)
            .WhereElementIsNotElementType()
            .Where(e => e.get_BoundingBox(null) != null)
            .ToList();
    }

    private static void Select(UIApplication ui, ClashRow row)
    {
        if (row == null)
            return;

        var uidoc = ui.ActiveUIDocument;
        if (uidoc == null)
            return;

        var ids = new List<ElementId> { Ids.Of(row.AId), Ids.Of(row.BId) };
        try
        {
            uidoc.Selection.SetElementIds(ids);
            uidoc.ShowElements(ids);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Не удалось выбрать элементы: " + ex.Message);
        }
    }

    private static void ShowView(UIApplication ui, ClashRow row)
    {
        if (row == null)
            return;

        var uidoc = ui.ActiveUIDocument;
        var doc = uidoc?.Document;
        if (doc == null)
            return;

        var elementA = doc.GetElement(Ids.Of(row.AId));
        var elementB = doc.GetElement(Ids.Of(row.BId));
        if (elementA == null || elementB == null)
            return;

        var familyType = ViewUtils.Get3dViewFamilyType(doc);
        if (familyType == null)
            return;

        using (var tx = new Transaction(doc, "N Plugin: вид пересечения"))
        {
            tx.Start();
            var view = View3D.CreateIsometric(doc, familyType.Id);
            view.Name = ViewUtils.UniqueViewName(ViewUtils.GetViewNames(doc),
                "Clash_" + FileNameTemplate.Sanitize(row.AName) + "_" + FileNameTemplate.Sanitize(row.BName));
            var box = ViewUtils.UnionBoundingBox(new[] { elementA, elementB }, 2.0);
            if (box != null)
                view.SetSectionBox(box);
            tx.Commit();

            var ids = new List<ElementId> { elementA.Id, elementB.Id };
            uidoc.Selection.SetElementIds(ids);
            uidoc.ShowElements(ids);
        }
    }

    private static void ExportExcel(ClashViewModel vm, IEnumerable<ClashRow> rows)
    {
        var list = rows?.ToList();
        if (list == null || list.Count == 0)
            return;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = "Clashes_" + DateTime.Now.ToString("yyyy-MM-dd") + ".xlsx",
            Title = "Сохранить отчёт"
        };
        if (dialog.ShowDialog() != true)
            return;

        try
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Пересечения");
            worksheet.Cell(1, 1).Value = "Элемент A";
            worksheet.Cell(1, 2).Value = "Система A";
            worksheet.Cell(1, 3).Value = "Элемент B";
            worksheet.Cell(1, 4).Value = "Система B";
            worksheet.Cell(1, 5).Value = "Уровень";
            worksheet.Row(1).Style.Font.Bold = true;

            for (var i = 0; i < list.Count; i++)
            {
                var row = list[i];
                worksheet.Cell(i + 2, 1).Value = row.AName;
                worksheet.Cell(i + 2, 2).Value = row.ASystem;
                worksheet.Cell(i + 2, 3).Value = row.BName;
                worksheet.Cell(i + 2, 4).Value = row.BSystem;
                worksheet.Cell(i + 2, 5).Value = row.Level;
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(dialog.FileName);
            vm.Status = "Отчёт сохранён: " + dialog.FileName;
        }
        catch (Exception ex)
        {
            vm.Status = "Ошибка экспорта Excel: " + ex.Message;
        }
    }
}
