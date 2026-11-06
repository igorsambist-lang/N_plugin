using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NPlugin.UI.Infrastructure;

namespace NPlugin.UI.ViewModels;

public class ModelRow : ViewModelBase
{
    public string Path { get; set; }

    private string _status = "В очереди";
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }
}

public class ModelExportSettings
{
    public List<string> Models { get; set; } = new List<string>();
    public string OutputDir { get; set; }
}

public class ModelExportViewModel : ViewModelBase
{
    public ObservableCollection<ModelRow> Models { get; } = new ObservableCollection<ModelRow>();

    private ModelRow _selectedModel;
    public ModelRow SelectedModel
    {
        get => _selectedModel;
        set => SetProperty(ref _selectedModel, value);
    }

    private string _outputDir;
    public string OutputDir
    {
        get => _outputDir;
        set => SetProperty(ref _outputDir, value);
    }

    private string _status = "Добавьте центральные модели (.rvt).";
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    private bool _busy;
    public bool Busy
    {
        get => _busy;
        set => SetProperty(ref _busy, value);
    }

    public event Action ExportRequested;

    public RelayCommand AddCommand { get; }
    public RelayCommand RemoveCommand { get; }
    public RelayCommand ExportCommand { get; }

    public ModelExportViewModel()
    {
        AddCommand = new RelayCommand(_ => AddFiles(), _ => !Busy);
        RemoveCommand = new RelayCommand(_ => RemoveSelected(), _ => !Busy && SelectedModel != null);
        ExportCommand = new RelayCommand(_ => ExportRequested?.Invoke(),
            _ => !Busy && Models.Count > 0 && !string.IsNullOrWhiteSpace(OutputDir));
    }

    private void AddFiles()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Модели Revit (*.rvt)|*.rvt",
            Multiselect = true,
            Title = "Выберите центральные модели"
        };
        if (dialog.ShowDialog() != true)
            return;

        foreach (var file in dialog.FileNames)
        {
            if (Models.Any(m => string.Equals(m.Path, file, StringComparison.OrdinalIgnoreCase)))
                continue;
            Models.Add(new ModelRow { Path = file });
        }

        Status = $"Моделей: {Models.Count}.";
    }

    private void RemoveSelected()
    {
        if (SelectedModel != null)
            Models.Remove(SelectedModel);
    }

    public ModelExportSettings CaptureSettings()
    {
        return new ModelExportSettings
        {
            Models = Models.Select(m => m.Path).ToList(),
            OutputDir = OutputDir
        };
    }

    public ModelRow FindRow(string path)
    {
        return Models.FirstOrDefault(m => string.Equals(m.Path, path, StringComparison.OrdinalIgnoreCase));
    }
}
