using System.Windows;

namespace NPlugin.UI.Views;

public partial class NavisworksWindow : Window
{
    public ViewModels.NavisworksViewModel VM { get; }

    public NavisworksWindow()
    {
        InitializeComponent();
        VM = new ViewModels.NavisworksViewModel();
        DataContext = VM;
    }

    private void BrowseNwcClick(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Выберите папку для NWC"
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            VM.NwcFolder = dialog.SelectedPath;
    }
}
