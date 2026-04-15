using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SMassTrans.Avalonia.ViewModels;

namespace SMassTrans.Avalonia.Views;

public partial class RouteTrackingSelectionView : UserControl
{
    private RouteTrackingSelectionViewModel _viewModel;

    public RouteTrackingSelectionView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        
        if (DataContext is RouteTrackingSelectionViewModel viewModel)
        {
            _viewModel = viewModel;
            _viewModel.RouteSelected += OnRouteSelected;
            _viewModel.LoadRoutes();
        }
    }

    private void RefreshClick(object sender, RoutedEventArgs e)
    {
        _viewModel?.LoadRoutes();
    }

    private void BackClick(object sender, RoutedEventArgs e)
    {
        // Navigate back - this would be handled by parent view
    }

    private void RouteDoubleClicked(object sender, RoutedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is RouteSelectionModel route)
        {
            _viewModel?.SelectRoute(route);
        }
    }

    private void OnRouteSelected(object sender, RouteSelectionModel route)
    {
        // The view model now handles navigation and tracking initialization
        Debug.WriteLine($"RouteTrackingSelectionView.OnRouteSelected: Route {route.RouteInfo.Name} {route.RouteInfo.Direction} selected");
    }
}
