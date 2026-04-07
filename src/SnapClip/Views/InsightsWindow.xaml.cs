using System.Windows;
using SnapClip.ViewModels;

namespace SnapClip.Views;

/// <summary>
/// Code-behind for the Usage Insights window.
/// </summary>
public partial class InsightsWindow : Window
{
    private readonly InsightsViewModel _viewModel;

    public InsightsWindow(InsightsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadInsightsAsync();
    }
}
