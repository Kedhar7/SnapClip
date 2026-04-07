using System.Windows;
using SnapClip.ViewModels;

namespace SnapClip.Views;

/// <summary>
/// Code-behind for the Settings window.
/// </summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
