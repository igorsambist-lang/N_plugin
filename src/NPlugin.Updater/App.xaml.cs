using System;
using System.Linq;
using System.Windows;

namespace NPlugin.Updater;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        NPlugin.Core.Paths.EnsureBaseDirs();
        NPlugin.Core.Log.Init(System.IO.Path.Combine(NPlugin.Core.Paths.LogsDir, "updater.log"));

        if (e.Args.Any(a => string.Equals(a, "/silent", StringComparison.OrdinalIgnoreCase)))
        {
            int exitCode;
            try
            {
                var executor = new Services.UpdateExecutor();
                executor.LogLine += msg => NPlugin.Core.Log.Info(msg);
                var result = executor.CheckAndInstallAsync(true).GetAwaiter().GetResult();
                NPlugin.Core.Log.Info("Silent-режим: " + result.Message);
                exitCode = string.IsNullOrEmpty(result.Error) ? 0 : 1;
            }
            catch (Exception ex)
            {
                NPlugin.Core.Log.Error("Silent-режим, фатальная ошибка: " + ex);
                exitCode = 1;
            }

            Shutdown(exitCode);
            return;
        }

        base.OnStartup(e);
        new MainWindow().Show();
    }
}
