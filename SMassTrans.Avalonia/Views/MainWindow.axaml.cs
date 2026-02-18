using Avalonia;
using Avalonia.Controls;

namespace SMassTrans.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        var topLevel = GetTopLevel(this);
        if (topLevel?.InsetsManager == null) return;
        topLevel.InsetsManager.DisplayEdgeToEdge = true;
        topLevel.InsetsManager.SafeAreaChanged += (s, args) =>
        {
            MainContent.Padding = args.SafeAreaPadding;
        };
        MainContent.Padding = topLevel.InsetsManager.SafeAreaPadding;
    }
}