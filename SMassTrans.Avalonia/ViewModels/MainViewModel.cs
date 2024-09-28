using System;
using Avalonia.Collections;
using Avalonia.Controls;
using ReactiveUI;
using SMassTrans.Avalonia.Views;
using SMassTrans.Shared;

namespace SMassTrans.Avalonia.ViewModels;

public class MainViewModel : ViewModelBase
{
    public Skopska client;
    public static EventHandler<LineRouteId> SetGpsLine;
    private AvaloniaList<IStop> _stops = new();
    public AvaloniaList<IStop> Stops{
        get=>_stops;
        set => this.RaiseAndSetIfChanged(ref _stops, value);
    }
    private AvaloniaList<IRouteInfo> _lines = new();
    public AvaloniaList<IRouteInfo> Lines{
        get=>_lines;
        set => this.RaiseAndSetIfChanged(ref _lines, value);
    }
    private Control _stopContent = new StopSelection();
    public Control StopContent{
        get=>_stopContent;
        set => this.RaiseAndSetIfChanged(ref _stopContent, value);
    }
    private Control? _prevStopContent;
    public Control? PrevStopContent{
        get=>_prevStopContent;
        set => this.RaiseAndSetIfChanged(ref _prevStopContent, value);
    }
    private Control _stopGPSContent = new StopSelectionGPS();
    public Control StopGPSContent{
        get=>_stopGPSContent;
        set => this.RaiseAndSetIfChanged(ref _stopGPSContent, value);
    }
    private Control? _prevStopGPSContent;
    public Control? PrevStopGPSContent{
        get=>_prevStopGPSContent;
        set => this.RaiseAndSetIfChanged(ref _prevStopGPSContent, value);
    }
    private AvaloniaList<UpcomingBusOnStop> _busesOnStop = new();
    public AvaloniaList<UpcomingBusOnStop> BusesOnStop{
        get=>_busesOnStop;
        set => this.RaiseAndSetIfChanged(ref _busesOnStop, value);
    }
      private string _mapName = "Map";
    public string MapName{
        get=>_mapName;
        set => this.RaiseAndSetIfChanged(ref _mapName, value);
    }
}

