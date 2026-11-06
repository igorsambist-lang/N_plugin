using System.Windows;

namespace NPlugin.UI.Views;

public partial class ClashWindow : Window
{
    public ViewModels.ClashViewModel VM { get; }

    public ClashWindow()
    {
        InitializeComponent();
        VM = new ViewModels.ClashViewModel();
        DataContext = VM;
    }
}
