using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using SnapClip.Helpers;
using SnapClip.ViewModels;

namespace SnapClip.Views;

/// <summary>
/// Code-behind for the main clip history window.
/// Only handles window management — all logic is in MainViewModel.
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        viewModel.PasteRequested += OnPasteRequested;
    }

    /// <summary>
    /// Shows the window and captures the previously active window handle for paste-back.
    /// </summary>
    public void ShowAndFocus()
    {
        // Remember which window was active before we opened
        _viewModel.PreviousWindowHandle = Win32Interop.GetForegroundWindow();

        // Position window centered on active monitor
        var screen = System.Windows.Forms.Screen.FromPoint(
            System.Windows.Forms.Cursor.Position);

        Left = screen.WorkingArea.Left + (screen.WorkingArea.Width - Width) / 2;
        Top = screen.WorkingArea.Top + (screen.WorkingArea.Height - Height) / 2;

        Show();
        Activate();

        // Focus search box
        SearchBox.Focus();
        SearchBox.SelectAll();
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        Hide();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                Hide();
                e.Handled = true;
                break;

            case Key.Enter when ClipListBox.SelectedItem is ClipItemViewModel clipVm:
                _viewModel.SelectClipCommand.Execute(clipVm);
                e.Handled = true;
                break;

            case Key.Delete when ClipListBox.SelectedItem is ClipItemViewModel deleteVm:
                _viewModel.DeleteClipCommand.Execute(deleteVm);
                e.Handled = true;
                break;

            case Key.Down when SearchBox.IsFocused:
                ClipListBox.Focus();
                if (ClipListBox.Items.Count > 0)
                    ClipListBox.SelectedIndex = 0;
                e.Handled = true;
                break;
        }
    }

    private void ClipItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { Tag: ClipItemViewModel clipVm })
        {
            _viewModel.SelectClipCommand.Execute(clipVm);
        }
    }

    private void ClipListBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && ClipListBox.SelectedItem is ClipItemViewModel clipVm)
        {
            _viewModel.SelectClipCommand.Execute(clipVm);
            e.Handled = true;
        }
    }

    private void CopyClip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: ClipItemViewModel clipVm })
        {
            Clipboard.SetText(clipVm.Content);
        }
    }

    private void MarkSensitive_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: ClipItemViewModel clipVm })
        {
            clipVm.IsEncrypted = true;
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            var settingsWindow = app.ServiceProvider.GetRequiredService<SettingsWindow>();
            settingsWindow.ShowDialog();
        }
    }

    private void InsightsButton_Click(object sender, RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            var insightsWindow = app.ServiceProvider.GetRequiredService<InsightsWindow>();
            insightsWindow.Show();
        }
    }

    private async void OnPasteRequested(object? sender, EventArgs e)
    {
        Hide();

        // Small delay to let window hide, then simulate paste into previous window
        await Task.Delay(100);

        if (_viewModel.PreviousWindowHandle != IntPtr.Zero)
        {
            Win32Interop.SetForegroundWindow(_viewModel.PreviousWindowHandle);
            await Task.Delay(50);
            System.Windows.Forms.SendKeys.SendWait("^v");
        }
    }
}
