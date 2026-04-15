using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SMassTrans.Avalonia.ViewModels;
#if ANDROID
using Android.App;
using Android.Content;
using AndroidX.Core.App;
#endif

namespace SMassTrans.Avalonia.Views;

public partial class UserRouteTrackingView : UserControl
{
    private UserRouteTrackingViewModel _viewModel;

    public UserRouteTrackingView()
    {
        InitializeComponent();
        
        // Subscribe to notification events
        if (DataContext is not UserRouteTrackingViewModel viewModel) return;
        _viewModel = viewModel;
      
    }

    


    private void StopTrackingClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserRouteTrackingViewModel viewModel)
        {
            viewModel.StopTrackingAsync();
        }
    }

    private void RefreshClick(object sender, RoutedEventArgs e)
    {
        // Refresh the UI by re-binding
        if (DataContext is UserRouteTrackingViewModel viewModel)
        {
            // The service will update automatically, but we can manually refresh if needed
        }
    }
}
