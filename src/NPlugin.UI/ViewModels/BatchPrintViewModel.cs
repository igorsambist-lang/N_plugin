using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NPlugin.UI.Infrastructure;

namespace NPlugin.UI.ViewModels;

public class SheetRow : ViewModelBase
{
    public long Id { get; set; }
    public string Number { get; set; }
    public string Name { get; set; }

    private bool _checked = true;
    public bool Checked
    {
        get => _checked;
        set => SetProperty(ref _checked, value);
    }
}

public class BatchPrintSettings
{
    public string OutputDir { get; set; }
    public string FileTemplate { get; set; }
    public bool Combine { get; set; }
    /// <summary>"PDF" или "DWG".</summary>
    public string Format { get; set; }
    public bool OpenAfter { get; set; }
    public List<SheetRow> Sheets { get; set; } = new List<SheetRow>();
}

public class BatchPrintViewModel : ViewModelBase
{
    public ObservableCollection<SheetRow> Sheets { get; } = new ObservableCollection<SheetRow>();

    private string _outputDir;
    public string OutputDir
    {
        get => _outputDir;
        set => SetProperty(ref _outputDir, value);
    }

    private string _fileTemplate = "{Номер}-{Имя}";
    public string FileTemplate
    {
        get => _fileTemplate;
        set => SetProperty(ref _fileTemplate, value);
    }

    private string _format = "PDF";
    public string Format
    {
        get => _format;
        set => SetProperty(ref _format, value);
    }

    private bool _combine;
    public bool Combine
    {
        get => _combine;
        set => SetProperty(ref _combine, value);
    }

    private bool _openAfter = true;
    public bool OpenAfter
    {
        get => _openAfter;
        set => SetProperty(ref _openAfter, value);
    }

    private string _status = "Обновите список листов.";
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

    private int _progress;
    public int Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }

    public event Action RefreshRequested;
    public event Action<BatchPrintSettings> PrintRequested;

    public RelayCommand RefreshCommand { get; }
    public RelayCommand PrintCommand { get; }
    public RelayCommand SelectAllCommand { get; }
    public RelayCommand SelectNoneCommand { get; }

    public BatchPrintViewModel()
    {
        RefreshCommand = new RelayCommand(_ => RefreshRequested?.Invoke(), _ => !Busy);
        PrintCommand = new RelayCommand(_ => PrintRequested?.Invoke(CaptureSettings()),
            _ => !Busy && !string.IsNullOrWhiteSpace(OutputDir));
        SelectAllCommand = new RelayCommand(_ => SetAll(true));
        SelectNoneCommand = new RelayCommand(_ => SetAll(false));
    }

    private void SetAll(bool value)
    {
        foreach (var row in Sheets)
            row.Checked = value;
    }

    public BatchPrintSettings CaptureSettings()
    {
        return new BatchPrintSettings
        {
            OutputDir = OutputDir,
            FileTemplate = FileTemplate,
            Combine = Combine,
            Format = Format,
            OpenAfter = OpenAfter,
            Sheets = new List<SheetRow>(Sheets)
        };
    }

    public void SetSheets(IEnumerable<SheetRow> sheets)
    {
        Sheets.Clear();
        foreach (var sheet in sheets)
            Sheets.Add(sheet);
        Status = $"Листов: {Sheets.Count}. Выберите листы и нажмите «Печать».";
    }

    public void ReportProgress(int done, int total, string status)
    {
        Progress = total > 0 ? done * 100 / total : 0;
        Status = status;
    }
}
