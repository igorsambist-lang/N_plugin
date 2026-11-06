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
public class Systems3dCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        NPluginApp.GetOrCreateWindow(() =>
        {
            var window = new Systems3dWindow();
            new Systems3dService(new RevitActionRunner()).Attach(window.VM);
            return window;
        });
        return Result.Succeeded;
    }
}
