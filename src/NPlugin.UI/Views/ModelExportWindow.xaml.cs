using System.Windows;

namespace NPlugin.UI.Views;

public partial class ModelExportWindow : Window
{
    public ViewModels.ModelExportViewModel VM { get; }

    public ModelExportWindow()
    {
        InitializeComponent();
        VM = new ViewModels.ModelExportViewModel();
        DataContext = VM;
    }

    private void BrowseOutputClick(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Выберите папку назначения"
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            VM.OutputDir = dialog.SelectedPath;
    }
}
