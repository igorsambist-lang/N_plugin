using System;
using System.Collections.ObjectModel;
using NPlugin.UI.Infrastructure;

namespace NPlugin.UI.ViewModels;

public class ViewItem
{
    public long Id { get; set; }
    public string Name { get; set; }
    public override string ToString() => Name;
}

public class NavisworksSettings
{
    public long SourceViewId { get; set; }
    public string NewName { get; set; }
    public bool HideAnnotations { get; set; }
    public bool HideImports { get; set; }
    public bool FineDetail { get; set; }
    public bool ExportNwc { get; set; }
    public string NwcFolder { get; set; }
}

public class NavisworksViewModel : ViewModelBase
{
    public ObservableCollection<ViewItem> Views { get; } = new ObservableCollection<ViewItem>();

    private ViewItem _selectedView;
    public ViewItem SelectedView
    {
        get => _selectedView;
        set => SetProperty(ref _selectedView, value);
    }

    private string _newName = "Navisworks";
    public string NewName
    {
        get => _newName;
        set => SetProperty(ref _newName, value);
    }

    private bool _hideAnnotations = true;
    public bool HideAnnotations
    {
        get => _hideAnnotations;
        set => SetProperty(ref _hideAnnotations, value);
    }

    private bool _hideImports = true;
    public bool HideImports
    {
        get => _hideImports;
        set => SetProperty(ref _hideImports, value);
    }

    private bool _fineDetail = true;
    public bool FineDetail
    {
        get => _fineDetail;
        set => SetProperty(ref _fineDetail, value);
    }

    private bool _exportNwc;
    public bool ExportNwc
    {
        get => _exportNwc;
        set => SetProperty(ref _exportNwc, value);
    }

    private string _nwcFolder;
    public string NwcFolder
    {
        get => _nwcFolder;
        set => SetProperty(ref _nwcFolder, value);
    }

    private string _status = "Обновите список 3D-видов.";
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
    public event Action<NavisworksSettings> CreateRequested;

    public RelayCommand RefreshCommand { get; }
    public RelayCommand CreateCommand { get; }

    public NavisworksViewModel()
    {
        RefreshCommand = new RelayCommand(_ => RefreshRequested?.Invoke(), _ => !Busy);
        CreateCommand = new RelayCommand(_ => CreateRequested?.Invoke(CaptureSettings()),
            _ => !Busy && SelectedView != null && !string.IsNullOrWhiteSpace(NewName)
                      && (!ExportNwc || !string.IsNullOrWhiteSpace(NwcFolder)));
    }

    public NavisworksSettings CaptureSettings()
    {
        return new NavisworksSettings
        {
            SourceViewId = SelectedView.Id,
            NewName = NewName.Trim(),
            HideAnnotations = HideAnnotations,
            HideImports = HideImports,
            FineDetail = FineDetail,
            ExportNwc = ExportNwc,
            NwcFolder = NwcFolder
        };
    }

    public void SetViews(System.Collections.Generic.IEnumerable<ViewItem> views)
    {
        Views.Clear();
        foreach (var view in views)
            Views.Add(view);
        Status = $"3D-видов: {Views.Count}. Выберите шаблонный вид.";
    }
}
