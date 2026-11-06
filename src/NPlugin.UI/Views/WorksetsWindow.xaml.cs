using System.Windows;

namespace NPlugin.UI.Views;

public partial class WorksetsWindow : Window
{
    public ViewModels.WorksetsViewModel VM { get; }

    public WorksetsWindow()
    {
        InitializeComponent();
        VM = new ViewModels.WorksetsViewModel();
        DataContext = VM;
    }
}
