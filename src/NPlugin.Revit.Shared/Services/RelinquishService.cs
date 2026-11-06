using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NPlugin.Infrastructure;

namespace NPlugin.Services;

/// <summary>
/// Освобождение рабочих наборов без входа в модель:
/// фоновое открытие центральной модели с закрытыми рабочими наборами,
/// затем RelinquishOwnership и закрытие без сохранения локальной копии.
/// </summary>
public class RelinquishService
{
    private readonly RevitActionRunner _runner;

    public RelinquishService(RevitActionRunner runner)
    {
        _runner = runner;
    }

    public void Attach(NPlugin.UI.ViewModels.RelinquishViewModel vm)
    {
        vm.ReleaseRequested += models => _runner.Run(ui => Release(ui, models, vm));
    }

    private static void Release(UIApplication ui, List<string> models, NPlugin.UI.ViewModels.RelinquishViewModel vm)
    {
        vm.Busy = true;
        try
        {
            foreach (var path in models)
            {
                var row = vm.FindRow(path);
                if (row == null)
                    continue;

                Document doc = null;
                try
                {
                    row.Status = "Открытие модели...";
                    var modelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(path);
                    var openOptions = new OpenOptions { Audit = false };
                    openOptions.SetOpenWorksetsConfiguration(
                        new WorksetConfiguration(WorksetConfigurationOption.CloseAllWorksets));
                    doc = ui.Application.OpenDocumentFile(modelPath, openOptions);

                    row.Status = "Освобождение наборов...";
                    var relinquishOptions = new RelinquishOptions(true)
                    {
                        CheckedOutElements = true,
                        UserWorksets = true,
                        FamilyWorksets = true,
                        ViewWorksets = true,
                        StandardWorksets = true
                    };
                    WorksharingUtils.RelinquishOwnership(doc, relinquishOptions, new TransactWithCentralOptions());
                    row.Status = "Готово";
                }
                catch (Exception ex)
                {
                    row.Status = "Ошибка: " + ex.Message;
                    NPlugin.Core.Log.Error($"Relinquish {path}: {ex}");
                }
                finally
                {
                    try
                    {
                        doc?.Close(false);
                    }
                    catch (Exception ex)
                    {
                        NPlugin.Core.Log.Warn($"Не удалось закрыть {path}: {ex.Message}");
                    }
                }
            }

            vm.Status = "Обработка завершена.";
        }
        finally
        {
            vm.Busy = false;
        }
    }
}
