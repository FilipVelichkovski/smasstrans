using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SMassTrans.Avalonia.ViewModels;
using SMassTrans.Shared;

namespace SMassTrans.Avalonia.Views;

public partial class StopSelectionLine : UserControl
{
    private MainViewModel _mainViewModel;
    private Tab bsource;

    public StopSelectionLine(MainViewModel mainViewModel, Tab source)
    {
        InitializeComponent();
        bsource = source;
        _mainViewModel=mainViewModel;
    }
    private void InputElement_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_mainViewModel is not { } a || sender is not StackPanel { DataContext: IStop stop }) return;
        a.GoForward(bsource, new StopInfo(stop, a, bsource));
    }
    private void BackRequested(object? sender, RoutedEventArgs e)
    {
        if (_mainViewModel is not { } a) return;
        e.Handled = true;
        a.GoBack(bsource);
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is not null)
        {
            topLevel.BackRequested -= BackRequested;
        }
        
    }
    private void TextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_mainViewModel is not MainViewModel mwv) return;
        if (!string.IsNullOrWhiteSpace(SearchBox.Text))
        {
            mwv.Stops = new AvaloniaList<IStop>(mwv.Stops.OrderBy(x => Search.ModifiedLevenshteinDistance(x.StopName.ToLowerInvariant(),SearchBox.Text.ToLowerInvariant()))); ;
        }
        else
        {
            mwv.Stops = new AvaloniaList<IStop>(mwv.Stops.OrderBy(x => x.StopNum));
        }
    }
}