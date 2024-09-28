using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using SMassTrans.Avalonia.ViewModels;
using SMassTrans.Shared;

namespace SMassTrans.Avalonia.Views;

public partial class StopSelection : UserControl
{
   

    public StopSelection()
    {
        InitializeComponent();
    }
    private void StopTap(object? sender, TappedEventArgs e)
    {
        if ((MainViewModel)DataContext is not { } a || sender is not StackPanel panel ||
            panel.DataContext is not IStop stop) return;
        a.PrevStopContent = a.StopContent;
        a.StopContent = new StopInfo(stop, a.client);
    }

    private void InputElement_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if ((MainViewModel)DataContext is not { } a || sender is not StackPanel panel ||
            panel.DataContext is not IStop stop) return;
        a.PrevStopContent = a.StopContent;
        a.StopContent = new StopInfo(stop, a.client);
    }

    private void TextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (DataContext is not MainViewModel mwv) return;
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