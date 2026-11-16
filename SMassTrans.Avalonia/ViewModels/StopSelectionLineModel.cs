using System.Collections.Generic;
using ReactiveUI;
using SMassTrans.Shared;

namespace SMassTrans.Avalonia.ViewModels;

public class StopSelectionLineModel:ViewModelBase
{
    
    private List<IStop> _linePageStops = new();
    public List<IStop> LinePageStops{
        get=>_linePageStops;
        set => this.RaiseAndSetIfChanged(ref _linePageStops, value);
    }
}