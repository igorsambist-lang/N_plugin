using System.Windows;

namespace NPlugin.UI.Views;

public partial class RelinquishWindow : Window
{
    public ViewModels.RelinquishViewModel VM { get; }

    public RelinquishWindow()
    {
        InitializeComponent();
        VM = new ViewModels.RelinquishViewModel();
        DataContext = VM;
    }
}
