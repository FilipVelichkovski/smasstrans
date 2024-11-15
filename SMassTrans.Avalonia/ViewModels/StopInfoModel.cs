using Avalonia.Collections;
using ReactiveUI;
using SMassTrans.Avalonia.Views;

namespace SMassTrans.Avalonia.ViewModels;

public class StopInfoModel:ViewModelBase
{
    private AvaloniaList<UpcomingBusOnStop> _busesOnStop = new();
    public AvaloniaList<UpcomingBusOnStop> BusesOnStop{
        get=>_busesOnStop;
        set => this.RaiseAndSetIfChanged(ref _busesOnStop, value);
    }
}