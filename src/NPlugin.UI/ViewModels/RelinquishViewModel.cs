using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NPlugin.UI.Infrastructure;

namespace NPlugin.UI.ViewModels;

public class RelinquishViewModel : ViewModelBase
{
    public ObservableCollection<ModelRow> Models { get; } = new ObservableCollection<ModelRow>();

    private ModelRow _selectedModel;
    public ModelRow SelectedModel
    {
        get => _selectedModel;
        set => SetProperty(ref _selectedModel, value);
    }

    private string _status = "Добавьте центральные модели, рабочие наборы которых нужно освободить.";
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

    public event Action<List<string>> ReleaseRequested;

    public RelayCommand AddCommand { get; }
    public RelayCommand RemoveCommand { get; }
    public RelayCommand ReleaseCommand { get; }

    public RelinquishViewModel()
    {
        AddCommand = new RelayCommand(_ => AddFiles(), _ => !Busy);
        RemoveCommand = new RelayCommand(_ =>
        {
            if (SelectedModel != null)
                Models.Remove(SelectedModel);
        }, _ => !Busy && SelectedModel != null);
        ReleaseCommand = new RelayCommand(_ => ReleaseRequested?.Invoke(Models.Select(m => m.Path).ToList()),
            _ => !Busy && Models.Count > 0);
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

    public ModelRow FindRow(string path)
    {
        return Models.FirstOrDefault(m => string.Equals(m.Path, path, StringComparison.OrdinalIgnoreCase));
    }
}
