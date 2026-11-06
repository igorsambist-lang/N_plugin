using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NPlugin.Infrastructure;
using NPlugin.UI.ViewModels;

namespace NPlugin.Services;

public class WorksetService
{
    private readonly RevitActionRunner _runner;

    public WorksetService(RevitActionRunner runner)
    {
        _runner = runner;
    }

    public void Attach(WorksetsViewModel vm)
    {
        vm.CreateRequested += names => _runner.Run(ui => Create(ui, names, vm));
    }

    private static void Create(UIApplication ui, IEnumerable<string> names, WorksetsViewModel vm)
    {
        var doc = ui.ActiveUIDocument?.Document;
        if (doc == null)
        {
            vm.SetResults(Array.Empty<WorksetResultRow>(), "Нет активного документа.");
            return;
        }

        if (!doc.IsWorkshared)
        {
            vm.SetResults(Array.Empty<WorksetResultRow>(),
                "Активная модель не является совместной — рабочие наборы недоступны. Откройте центральную модель.");
            return;
        }

        var nameList = names?.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()).ToList()
                       ?? new List<string>();
        if (nameList.Count == 0)
            return;

        vm.Busy = true;
        try
        {
            var existing = GetWorksetNames(doc);
            var results = new List<WorksetResultRow>();
            var created = 0;
            var skipped = 0;

            using (var tx = new Transaction(doc, "N Plugin: создание рабочих наборов"))
            {
                tx.Start();
                foreach (var name in nameList)
                {
                    if (existing.Contains(name))
                    {
                        results.Add(new WorksetResultRow { Name = name, Status = "Уже существует" });
                        skipped++;
                        continue;
                    }

                    try
                    {
                        Workset.Create(doc, name);
                        existing.Add(name);
                        results.Add(new WorksetResultRow { Name = name, Status = "Создан" });
                        created++;
                    }
                    catch (Exception ex)
                    {
                        results.Add(new WorksetResultRow { Name = name, Status = "Ошибка: " + ex.Message });
                    }
                }

                tx.Commit();
            }

            vm.SetResults(results,
                $"Готово. Создано: {created}, уже существовало: {skipped}. " +
                "Сохраните модель с синхронизацией, чтобы изменения попали на центральную.");
        }
        finally
        {
            vm.Busy = false;
        }
    }

    private static HashSet<string> GetWorksetNames(Document doc)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Workset workset in new FilteredWorksetCollector(doc).OfKind(WorksetKind.UserWorkset))
            names.Add(workset.Name);
        return names;
    }
}
