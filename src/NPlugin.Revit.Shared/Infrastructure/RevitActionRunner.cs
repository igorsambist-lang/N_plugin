using System;
using Autodesk.Revit.UI;

namespace NPlugin.Infrastructure;

/// <summary>
/// Выполняет действия в контексте Revit через ExternalEvent. Окна создаются на потоке Revit,
/// поэтому обработчик события и окна работают в одном потоке и обмен данными с ViewModel безопасен.
/// </summary>
public class RevitActionRunner : IExternalEventHandler
{
    private readonly object _gate = new object();
    private Action<UIApplication> _action;

    public RevitActionRunner()
    {
        Event = ExternalEvent.Create(this);
    }

    public ExternalEvent Event { get; }

    public void Run(Action<UIApplication> action)
    {
        lock (_gate)
        {
            _action = action;
        }

        Event.Raise();
    }

    public void Execute(UIApplication app)
    {
        Action<UIApplication> action;
        lock (_gate)
        {
            action = _action;
            _action = null;
        }

        try
        {
            action?.Invoke(app);
        }
        catch (Exception ex)
        {
            NPlugin.Core.Log.Error("Команда завершилась с ошибкой: " + ex);
            TaskDialog.Show("N Plugin", "Ошибка выполнения команды:\n" + ex.Message);
        }
    }

    public string GetName()
    {
        return "NPluginActionRunner";
    }
}
