using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using SMassTrans.Avalonia.ViewModels;
using SMassTrans.Shared;

namespace SMassTrans.Avalonia.Views;

public partial class StopSelection : UserControl
{

    public StopSelection()
    {
        InitializeComponent();
    }

    private void InputElement_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (DataContext is not MainViewModel a || sender is not StackPanel { DataContext: IStop stop }) return;
        a.GoForward(Tab.Stop,  new StopInfo(stop, a, Tab.Stop));
    }

    private void TextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (DataContext is not MainViewModel mwv) return;
        if (!string.IsNullOrWhiteSpace(SearchBox.Text))
        {
            mwv.Stops = new AvaloniaList<IStop>(mwv.Stops.OrderBy(x => Search.ModifiedLevenshteinDistance(x.StopName.ToLowerInvariant(),SearchBox.Text.ToLowerInvariant()))); 
        }
        else
        {
            mwv.Stops = new AvaloniaList<IStop>(mwv.Stops.OrderBy(x => x.StopNum));
        }
    }
}