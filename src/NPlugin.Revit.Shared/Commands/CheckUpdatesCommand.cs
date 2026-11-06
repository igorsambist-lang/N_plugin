using System.Diagnostics;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace NPlugin.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class CheckUpdatesCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var exePath = NPlugin.Core.Paths.UpdaterExePath;
        if (!File.Exists(exePath))
        {
            TaskDialog.Show("N Plugin",
                "Программа обновления не найдена:\n" + exePath + "\n\nУстановите плагин установочным скриптом (install).");
            return Result.Failed;
        }

        Process.Start(new ProcessStartInfo { FileName = exePath, UseShellExecute = true });
        return Result.Succeeded;
    }
}
