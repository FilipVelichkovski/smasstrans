using System.Collections.Generic;
using ReactiveUI;
using SMassTrans.Shared;

namespace SMassTrans.Avalonia.ViewModels;

public class StopSelectionLineModel:ViewModelBase
{
    
    private List<SkopskaStop> _linePageStops = new();
    public List<SkopskaStop> LinePageStops{
        get=>_linePageStops;
        set => this.RaiseAndSetIfChanged(ref _linePageStops, value);
    }
}