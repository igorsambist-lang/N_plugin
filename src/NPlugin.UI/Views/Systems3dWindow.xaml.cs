using System.Windows;

namespace NPlugin.UI.Views;

public partial class Systems3dWindow : Window
{
    public ViewModels.Systems3dViewModel VM { get; }

    public Systems3dWindow()
    {
        InitializeComponent();
        VM = new ViewModels.Systems3dViewModel();
        DataContext = VM;
    }
}
