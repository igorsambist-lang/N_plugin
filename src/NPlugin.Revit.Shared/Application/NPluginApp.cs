using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using Autodesk.Revit.UI;

namespace NPlugin.Application;

public class NPluginApp : IExternalApplication
{
    internal static readonly Dictionary<Type, Window> OpenWindows = new Dictionary<Type, Window>();

    public Result OnStartup(UIControlledApplication application)
    {
        try
        {
            System.IO.Directory.CreateDirectory(NPlugin.Core.Paths.LogsDir);
            NPlugin.Core.Log.Init(System.IO.Path.Combine(NPlugin.Core.Paths.LogsDir, "plugin.log"));
        }
        catch
        {
            // логирование недоступно — не критично
        }

        RibbonBuilder.Build(application);
        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        return Result.Succeeded;
    }

    /// <summary>Показывает modeless-окно одного экземпляра на тип; при повторном вызове активирует существующее.</summary>
    public static T GetOrCreateWindow<T>(Func<T> factory) where T : Window
    {
        if (OpenWindows.TryGetValue(typeof(T), out var existing))
        {
            if (existing.IsVisible)
            {
                existing.Activate();
                return (T)existing;
            }

            OpenWindows.Remove(typeof(T));
        }

        var window = factory();
        OpenWindows[typeof(T)] = window;
        window.Closed += (s, e) => OpenWindows.Remove(typeof(T));
        try
        {
            new WindowInteropHelper(window).Owner = Process.GetCurrentProcess().MainWindowHandle;
        }
        catch
        {
            // владелец недоступен — окно всё равно покажется
        }

        window.Show();
        return window;
    }
}
