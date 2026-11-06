using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NPlugin.UI.Infrastructure;

namespace NPlugin.UI.ViewModels;

public class CategoryItem
{
    /// <summary>Значение BuiltInCategory.</summary>
    public int Id { get; set; }
    public string Name { get; set; }
    public override string ToString() => Name;
}

public class ClashRow : ViewModelBase
{
    public long AId { get; set; }
    public long BId { get; set; }
    public string AName { get; set; }
    public string BName { get; set; }
    public string ASystem { get; set; }
    public string BSystem { get; set; }
    public string Level { get; set; }

    private bool _checked = true;
    public bool Checked
    {
        get => _checked;
        set => SetProperty(ref _checked, value);
    }
}

public class ClashSettings
{
    public int LeftCategoryId { get; set; }
    public int RightCategoryId { get; set; }
}

public class ClashViewModel : ViewModelBase
{
    public ObservableCollection<CategoryItem> LeftCategories { get; } = new ObservableCollection<CategoryItem>();
    public ObservableCollection<CategoryItem> RightCategories { get; } = new ObservableCollection<CategoryItem>();
    public ObservableCollection<ClashRow> Clashes { get; } = new ObservableCollection<ClashRow>();

    private CategoryItem _leftCategory;
    public CategoryItem LeftCategory
    {
        get => _leftCategory;
        set => SetProperty(ref _leftCategory, value);
    }

    private CategoryItem _rightCategory;
    public CategoryItem RightCategory
    {
        get => _rightCategory;
        set => SetProperty(ref _rightCategory, value);
    }

    private ClashRow _selectedClash;
    public ClashRow SelectedClash
    {
        get => _selectedClash;
        set => SetProperty(ref _selectedClash, value);
    }

    private string _status = "Выберите пары категорий и запустите проверку.";
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

    public bool CancelRequested { get; private set; }

    public event Action<ClashSettings> RunRequested;
    public event Action<ClashRow> SelectRequested;
    public event Action<ClashRow> ShowViewRequested;
    public event Action<IEnumerable<ClashRow>> ExportExcelRequested;

    public RelayCommand RunCommand { get; }
    public RelayCommand SelectCommand { get; }
    public RelayCommand ShowViewCommand { get; }
    public RelayCommand ExportExcelCommand { get; }
    public RelayCommand CancelCommand { get; }

    public ClashViewModel()
    {
        RunCommand = new RelayCommand(_ =>
        {
            CancelRequested = false;
            RunRequested?.Invoke(CaptureSettings());
        }, _ => !Busy && LeftCategory != null && RightCategory != null);

        SelectCommand = new RelayCommand(_ => SelectRequested?.Invoke(SelectedClash),
            _ => SelectedClash != null && !Busy);
        ShowViewCommand = new RelayCommand(_ => ShowViewRequested?.Invoke(SelectedClash),
            _ => SelectedClash != null && !Busy);
        ExportExcelCommand = new RelayCommand(_ => ExportExcelRequested?.Invoke(Clashes),
            _ => Clashes.Count > 0 && !Busy);
        CancelCommand = new RelayCommand(_ => CancelRequested = true, _ => Busy);
    }

    public ClashSettings CaptureSettings()
    {
        return new ClashSettings
        {
            LeftCategoryId = LeftCategory.Id,
            RightCategoryId = RightCategory.Id
        };
    }

    public void SetCategories(IEnumerable<CategoryItem> items)
    {
        LeftCategories.Clear();
        RightCategories.Clear();
        foreach (var item in items)
        {
            LeftCategories.Add(item);
            RightCategories.Add(new CategoryItem { Id = item.Id, Name = item.Name });
        }

        if (LeftCategories.Count > 0) LeftCategory = LeftCategories[0];
        if (RightCategories.Count > 1) RightCategory = RightCategories[1];
    }

    public void SetClashes(IEnumerable<ClashRow> rows)
    {
        Clashes.Clear();
        foreach (var row in rows)
            Clashes.Add(row);
        Status = $"Пересечений: {Clashes.Count}.";
    }

    public void ReportProgress(int done, int total, string status)
    {
        Progress = total > 0 ? done * 100 / total : 0;
        Status = status;
    }
}
