using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NPlugin.Application;
using NPlugin.Infrastructure;
using NPlugin.Services;
using NPlugin.UI.Views;

namespace NPlugin.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class RelinquishCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        NPluginApp.GetOrCreateWindow(() =>
        {
            var window = new RelinquishWindow();
            new RelinquishService(new RevitActionRunner()).Attach(window.VM);
            return window;
        });
        return Result.Succeeded;
    }
}
