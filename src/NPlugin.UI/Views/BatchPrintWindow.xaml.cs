using System.Windows;

namespace NPlugin.UI.Views;

public partial class BatchPrintWindow : Window
{
    public ViewModels.BatchPrintViewModel VM { get; }

    public BatchPrintWindow()
    {
        InitializeComponent();
        VM = new ViewModels.BatchPrintViewModel();
        DataContext = VM;
    }

    private void BrowseOutputClick(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Выберите папку для файлов"
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            VM.OutputDir = dialog.SelectedPath;
    }
}
