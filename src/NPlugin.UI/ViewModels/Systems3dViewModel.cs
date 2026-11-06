using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NPlugin.UI.Infrastructure;

namespace NPlugin.UI.ViewModels;

public class SystemRow : ViewModelBase
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Discipline { get; set; }
    public int ElementCount { get; set; }

    private bool _checked = true;
    public bool Checked
    {
        get => _checked;
        set => SetProperty(ref _checked, value);
    }
}

public class Systems3dSettings
{
    public string Prefix { get; set; }
    public bool SetSectionBox { get; set; }
    public List<long> SystemIds { get; set; } = new List<long>();
}

public class Systems3dViewModel : ViewModelBase
{
    public ObservableCollection<SystemRow> Systems { get; } = new ObservableCollection<SystemRow>();
    public ObservableCollection<string> Results { get; } = new ObservableCollection<string>();

    private string _prefix = "";
    public string Prefix
    {
        get => _prefix;
        set => SetProperty(ref _prefix, value);
    }

    private bool _setSectionBox = true;
    public bool SetSectionBox
    {
        get => _setSectionBox;
        set => SetProperty(ref _setSectionBox, value);
    }

    private string _status = "Обновите список инженерных систем.";
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

    public event Action RefreshRequested;
    public event Action<Systems3dSettings> CreateRequested;

    public RelayCommand RefreshCommand { get; }
    public RelayCommand CreateCommand { get; }
    public RelayCommand SelectAllCommand { get; }
    public RelayCommand SelectNoneCommand { get; }

    public Systems3dViewModel()
    {
        RefreshCommand = new RelayCommand(_ => RefreshRequested?.Invoke(), _ => !Busy);
        CreateCommand = new RelayCommand(_ => CreateRequested?.Invoke(CaptureSettings()),
            _ => !Busy && Systems.Any(s => s.Checked));
        SelectAllCommand = new RelayCommand(_ => SetAll(true));
        SelectNoneCommand = new RelayCommand(_ => SetAll(false));
    }

    private void SetAll(bool value)
    {
        foreach (var row in Systems)
            row.Checked = value;
    }

    public Systems3dSettings CaptureSettings()
    {
        return new Systems3dSettings
        {
            Prefix = Prefix ?? "",
            SetSectionBox = SetSectionBox,
            SystemIds = Systems.Where(s => s.Checked).Select(s => s.Id).ToList()
        };
    }

    public void SetSystems(IEnumerable<SystemRow> systems)
    {
        Systems.Clear();
        foreach (var system in systems)
            Systems.Add(system);
        Status = $"Систем: {Systems.Count}. Отметьте системы и нажмите «Создать виды».";
    }

    public void AddResult(string message)
    {
        Results.Insert(0, message);
    }
}
