using System;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NPlugin.Infrastructure;
using NPlugin.UI.ViewModels;

namespace NPlugin.Services;

public class NavisworksViewService
{
    private readonly RevitActionRunner _runner;

    public NavisworksViewService(RevitActionRunner runner)
    {
        _runner = runner;
    }

    public void Attach(NavisworksViewModel vm)
    {
        vm.RefreshRequested += () => _runner.Run(ui => LoadViews(ui, vm));
        vm.CreateRequested += settings => _runner.Run(ui => Create(ui, settings, vm));
    }

    private static void LoadViews(UIApplication ui, NavisworksViewModel vm)
    {
        var doc = ui.ActiveUIDocument?.Document;
        if (doc == null)
        {
            vm.Status = "Нет активного документа.";
            return;
        }

        var views = new FilteredElementCollector(doc)
            .OfClass(typeof(View3D))
            .Cast<View3D>()
            .Where(v => !v.IsTemplate)
            .OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase)
            .Select(v => new ViewItem { Id = Ids.GetValue(v.Id), Name = v.Name });
        vm.SetViews(views);
    }

    private static void Create(UIApplication ui, NavisworksSettings settings, NavisworksViewModel vm)
    {
        var doc = ui.ActiveUIDocument?.Document;
        if (doc == null)
        {
            vm.Status = "Нет активного документа.";
            return;
        }

        vm.Busy = true;
        View3D created;
        try
        {
            var existingNames = ViewUtils.GetViewNames(doc);
            var viewName = ViewUtils.UniqueViewName(existingNames, settings.NewName);

            using (var tx = new Transaction(doc, "N Plugin: вид Navisworks"))
            {
                tx.Start();

                var view = CreateView(doc, settings.SourceViewId);
                view.Name = viewName;
                if (settings.FineDetail)
                    view.DetailLevel = ViewDetailLevel.Fine;
                HideCategories(doc, view, settings.HideAnnotations, settings.HideImports);

                tx.Commit();
                created = view;
            }

            if (settings.ExportNwc)
            {
                vm.Status = "Экспорт NWC...";
                var options = new NavisworksExportOptions
                {
                    ExportScope = NavisworksExportScope.View,
                    ViewId = created.Id,
                    Coordinates = NavisworksCoordinates.Shared
                };
                doc.Export(settings.NwcFolder, created.Name, options);
            }

            vm.Status = settings.ExportNwc
                ? $"Создан вид «{created.Name}», экспортирован NWC."
                : $"Создан вид «{created.Name}».";
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

    private static View3D CreateView(Document doc, long sourceViewId)
    {
        var source = doc.GetElement(Ids.Of(sourceViewId)) as View3D;
        if (source != null && !source.IsTemplate)
        {
            var duplicateId = source.Duplicate(ViewDuplicateOption.Duplicate);
            return (View3D)doc.GetElement(duplicateId);
        }

        var familyType = ViewUtils.Get3dViewFamilyType(doc);
        if (familyType == null)
            throw new InvalidOperationException("В модели нет типа 3D-вида.");
        return View3D.CreateIsometric(doc, familyType.Id);
    }

    private static void HideCategories(Document doc, View3D view, bool hideAnnotations, bool hideImports)
    {
        if (!hideAnnotations && !hideImports)
            return;

        var importCategory = doc.Settings.Categories.get_Item(BuiltInCategory.OST_ImportObjectStyles);
        foreach (Category category in doc.Settings.Categories)
        {
            if (hideImports && importCategory != null && category.Id == importCategory.Id)
                view.SetCategoryHidden(category.Id, true);
            else if (hideAnnotations && category.CategoryType == CategoryType.Annotation)
                view.SetCategoryHidden(category.Id, true);
        }

        var cameras = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Cameras);
        if (cameras != null)
            view.SetCategoryHidden(cameras.Id, true);
    }
}
