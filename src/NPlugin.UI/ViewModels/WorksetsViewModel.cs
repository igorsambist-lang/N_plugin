using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NPlugin.UI.Infrastructure;

namespace NPlugin.UI.ViewModels;

public class WorksetResultRow
{
    public string Name { get; set; }
    public string Status { get; set; }
}

public class WorksetsViewModel : ViewModelBase
{
    private string _namesText;
    public string NamesText
    {
        get => _namesText;
        set => SetProperty(ref _namesText, value);
    }

    public ObservableCollection<WorksetResultRow> Results { get; } = new ObservableCollection<WorksetResultRow>();

    private string _status = "Введите имена рабочих наборов, по одному в строке.";
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

    public event Action<IEnumerable<string>> CreateRequested;

    public RelayCommand CreateCommand { get; }

    public WorksetsViewModel()
    {
        CreateCommand = new RelayCommand(_ => CreateRequested?.Invoke(ParseNames()),
            _ => !Busy && !string.IsNullOrWhiteSpace(NamesText));
    }

    private IEnumerable<string> ParseNames()
    {
        var names = new List<string>();
        if (NamesText == null)
            return names;

        foreach (var line in NamesText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var name = line.Trim();
            if (name.Length > 0 && !names.Contains(name))
                names.Add(name);
        }

        return names;
    }

    public void SetResults(IEnumerable<WorksetResultRow> rows, string status)
    {
        Results.Clear();
        foreach (var row in rows)
            Results.Add(row);
        Status = status;
    }
}
