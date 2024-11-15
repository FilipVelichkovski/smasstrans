using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SMassTrans.Avalonia.ViewModels;
using SMassTrans.Shared;

namespace SMassTrans.Avalonia.Views;

public partial class LineView : UserControl
{
    public LineView()
    {
        InitializeComponent();
    }
    private void LineDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control { DataContext: IRouteInfo context } a)
        {
            a.ContextMenu.Open();
        }
    }
    private void LineGPS(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: IRouteInfo context })
        {
            MainViewModel.SetGpsLine.Invoke(this,  context.LineRouteId);
        }
    }
    private void CopyStops(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { DataContext: IRouteInfo context }) return;
        var topLevel = TopLevel.GetTopLevel(this);
        StringBuilder s = new();
        s.AppendLine($"{context.Name}: ");
        if (context is RouteInfo routeInfo)
        {
            var stops = routeInfo.StopIds
                .Select(x => ((MainViewModel)DataContext).client.GetStop(x).GetAwaiter().GetResult()).ToList();
            foreach (var stop in stops)
            {
                s.AppendLine($"{stop.StopName}");
            }
        }
        topLevel.Clipboard.SetTextAsync(s.ToString());
    }
    private void ViewStops(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { DataContext: IRouteInfo context }) return;
        if (context is not RouteInfo routeInfo || DataContext is not MainViewModel mainViewModel) return;
        var stops = routeInfo.StopIds
            .Select(x => mainViewModel.client.GetStop(x).GetAwaiter().GetResult()).ToList();
        mainViewModel.GoForward(Tab.Line,new StopSelectionLine(mainViewModel, Tab.Line)
        {
            DataContext = new StopSelectionLineModel()
            {
                LinePageStops = stops,
            }
        });
    }

}