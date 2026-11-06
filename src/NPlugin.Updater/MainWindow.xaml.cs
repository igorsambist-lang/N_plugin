using System;
using System.Windows;
using NPlugin.Core;
using NPlugin.Updater.Services;

namespace NPlugin.Updater;

public partial class MainWindow : Window
{
    private readonly UpdateExecutor _executor = new UpdateExecutor();
    private bool _busy;

    public MainWindow()
    {
        InitializeComponent();
        _executor.LogLine += AddLog;
        ServerUrlBox.Text = UpdaterConfig.Load(Paths.ConfigPath).ServerUrl;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var local = LocalVersion.Read(Paths.VersionPath);
        VersionLabel.Text = $"Версия: {local ?? "не установлена"}";
        _ = RunCheckAsync(false);
    }

    private void AddLog(string message)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            LogList.Items.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");
            Log.Info(message);
        }));
    }

    private void SaveConfigClick(object sender, RoutedEventArgs e)
    {
        var config = UpdaterConfig.Load(Paths.ConfigPath);
        config.ServerUrl = ServerUrlBox.Text.Trim();
        config.Save(Paths.ConfigPath);
        AddLog("Настройки сохранены.");
    }

    private void CheckClick(object sender, RoutedEventArgs e)
    {
        _ = RunCheckAsync(false);
    }

    private void UpdateClick(object sender, RoutedEventArgs e)
    {
        _ = RunCheckAsync(true);
    }

    private async System.Threading.Tasks.Task RunCheckAsync(bool install)
    {
        if (_busy)
            return;

        _busy = true;
        UpdateButton.IsEnabled = false;
        StatusLabel.Text = install ? "Установка обновления..." : "Проверка обновлений...";
        try
        {
            var result = await _executor.CheckAndInstallAsync(install);
            StatusLabel.Text = result.Message;
            VersionLabel.Text = $"Версия: {(result.Updated ? result.ServerVersion : result.LocalVersion) ?? "—"}";

            if (result.UpdateAvailable && !result.Updated && !result.Deferred)
                UpdateButton.IsEnabled = true;
        }
        finally
        {
            _busy = false;
        }
    }
}
