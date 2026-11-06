using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NPlugin.Core;
using NPlugin.Infrastructure;
using NPlugin.UI.ViewModels;

namespace NPlugin.Services;

public class Systems3dService
{
    private readonly RevitActionRunner _runner;

    public Systems3dService(RevitActionRunner runner)
    {
        _runner = runner;
    }

    public void Attach(Systems3dViewModel vm)
    {
        vm.RefreshRequested += () => _runner.Run(ui => LoadSystems(ui, vm));
        vm.CreateRequested += settings => _runner.Run(ui => Create(ui, settings, vm));
    }

    private static void LoadSystems(UIApplication ui, Systems3dViewModel vm)
    {
        var doc = ui.ActiveUIDocument?.Document;
        if (doc == null)
        {
            vm.Status = "Нет активного документа.";
            return;
        }

        var systems = new FilteredElementCollector(doc)
            .OfClass(typeof(MEPSystem))
            .Cast<MEPSystem>()
            .Where(s => !string.IsNullOrEmpty(s.Name) && s.Elements != null && s.Elements.Size > 0)
            .Where(s => MepUtils.Discipline(s) != "Прочее")
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .Select(s => new SystemRow
            {
                Id = Ids.GetValue(s.Id),
                Name = s.Name,
                Discipline = MepUtils.Discipline(s),
                ElementCount = s.Elements.Size
            });

        vm.SetSystems(systems);
    }

    private static void Create(UIApplication ui, Systems3dSettings settings, Systems3dViewModel vm)
    {
        var doc = ui.ActiveUIDocument?.Document;
        if (doc == null)
        {
            vm.Status = "Нет активного документа.";
            return;
        }

        var familyType = ViewUtils.Get3dViewFamilyType(doc);
        if (familyType == null)
        {
            vm.Status = "В модели нет типа 3D-вида.";
            return;
        }

        vm.Busy = true;
        try
        {
            var created = 0;
            var existingNames = ViewUtils.GetViewNames(doc);

            using (var tx = new Transaction(doc, "N Plugin: 3D виды по системам"))
            {
                tx.Start();
                foreach (var systemId in settings.SystemIds)
                {
                    var system = doc.GetElement(Ids.Of(systemId)) as MEPSystem;
                    if (system == null)
                        continue;

                    var elements = MepUtils.GetSystemElements(system);
                    if (elements.Count == 0)
                        continue;

                    var view = View3D.CreateIsometric(doc, familyType.Id);
                    var baseName = FileNameTemplate.Sanitize(
                        (settings.Prefix ?? "") + MepUtils.Discipline(system) + "_" + system.Name);
                    view.Name = ViewUtils.UniqueViewName(existingNames, baseName);

                    if (settings.SetSectionBox)
                    {
                        var box = ViewUtils.UnionBoundingBox(elements, 1.5);
                        if (box != null)
                            view.SetSectionBox(box);
                    }

                    view.DetailLevel = ViewDetailLevel.Fine;
                    created++;
                    vm.AddResult($"Создан вид «{view.Name}» ({elements.Count} элементов)");
                }

                tx.Commit();
            }

            vm.Status = $"Создано видов: {created}.";
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
}
