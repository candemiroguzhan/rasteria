using System.Windows;
using MahApps.Metro.IconPacks;
using Rasteria.UI.ViewModels;

namespace Rasteria.UI;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.RasterLoaded += (_, _) => RasterMap.FitToFullExtent();
        viewModel.FitRasterRequest += (_, _) => RasterMap.FitToFullExtent();
        viewModel.ZoomRequest += (_, factor) => RasterMap.ZoomBy(factor);
        StateChanged += (_, _) => UpdateMaximizeRestoreIcon();
        UpdateMaximizeRestoreIcon();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void UpdateMaximizeRestoreIcon()
    {
        MaximizeRestoreIcon.Kind = WindowState == WindowState.Maximized
            ? PackIconMaterialKind.WindowRestore
            : PackIconMaterialKind.WindowMaximize;
    }
}
