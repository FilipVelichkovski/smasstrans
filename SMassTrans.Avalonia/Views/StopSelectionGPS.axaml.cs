using System;
using System.Globalization;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SMassTrans.Avalonia.ViewModels;
using SMassTrans.Shared;

namespace SMassTrans.Avalonia.Views;

public partial class StopSelectionGPS : UserControl
{
    public StopSelectionGPS()
    {
        InitializeComponent();
    }
    private void StopTap(object? sender, TappedEventArgs e)
    {
        if (DataContext is not MainViewModel a || sender is not StackPanel { DataContext: IStop stop }) return;
        a.GoForward(Tab.StopGPS,  new StopInfo(stop,a, Tab.StopGPS));
    }

    private void InputElement_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (DataContext is not MainViewModel a || sender is not StackPanel { DataContext: IStop stop }) return;
        a.GoForward(Tab.StopGPS,  new StopInfo(stop,a, Tab.StopGPS));
    }

    private void TextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (DataContext is not MainViewModel mwv) return;
        if (double.TryParse(Lat.Text, out var lat) && double.TryParse(Long.Text, out var longi) )
        {
            mwv.Stops = new AvaloniaList<IStop>(mwv.Stops.OrderBy(x =>CoordinateHelper.GetDistanceTo(lat, longi, x.Lat, x.Lon)));
        }
     
    }
 
    private async void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        var (l1, l2) = await LocationGatherer.GetLoc();
        Lat.Text = l1.ToString(CultureInfo.InvariantCulture);
        Long.Text = l2.ToString(CultureInfo.InvariantCulture);

    }
}