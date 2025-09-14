using Avalonia.Controls;
using ReactiveUI;

namespace SMassTrans.Avalonia.ViewModels;

public class RouteFindViewModel : ViewModelBase
{
    private Control? _routeFindContent = null;
    public Control? RouteFindContent
    {
        get => _routeFindContent;
        set => this.RaiseAndSetIfChanged(ref _routeFindContent, value);
    }

    public RouteFindViewModel()
    {
        // Initialize with a default view if needed
    }
}
