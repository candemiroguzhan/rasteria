using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rasteria.Core.Geometry;
using Rasteria.Core.Layers;
using Rasteria.Core.Rasters;
using Rasteria.Infrastructure.Coordinates;
using Rasteria.UI.Imaging;
using Rasteria.UI.Services;
using Serilog;

namespace Rasteria.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IFileDialogService _fileDialogService;
    private readonly CachedTileSourceFactory _tileSourceFactory;
    private readonly IRasterSourceFactory _rasterSourceFactory;
    private readonly CoordinateService _coordinateService;

    [ObservableProperty]
    private RasterMetadata? _activeRasterMetadata;

    [ObservableProperty]
    private LayerViewModel? _selectedLayer;

    [ObservableProperty]
    private GeoCoordinate _mouseImagePosition;

    [ObservableProperty]
    private double _zoomLevel = 1;

    [ObservableProperty]
    private string _status = "Ready";

    [ObservableProperty]
    private string _loadingState = "Idle";

    [ObservableProperty]
    private string _activeLayerText = "-";

    [ObservableProperty]
    private bool _isLayerPanelOpen = true;

    [ObservableProperty]
    private bool _isMetadataPanelOpen = true;

    public MainWindowViewModel(
        IFileDialogService fileDialogService,
        CachedTileSourceFactory tileSourceFactory,
        IRasterSourceFactory rasterSourceFactory,
        CoordinateService coordinateService)
    {
        _fileDialogService = fileDialogService;
        _tileSourceFactory = tileSourceFactory;
        _rasterSourceFactory = rasterSourceFactory;
        _coordinateService = coordinateService;
    }

    public ObservableCollection<LayerViewModel> Layers { get; } = [];

    public string FileName => ActiveRasterMetadata is null ? "-" : Path.GetFileName(ActiveRasterMetadata.FilePath);
    public string MetadataTypeText => ActiveRasterMetadata is null ? "-" : "Raster";
    public string RasterSizeText => ActiveRasterMetadata is null ? "-" : $"{ActiveRasterMetadata.Width:N0} x {ActiveRasterMetadata.Height:N0}";
    public string BandCountText => ActiveRasterMetadata?.BandCount.ToString() ?? "-";
    public string CrsText => ActiveRasterMetadata?.CoordinateSystem.DisplayName ?? "-";
    public string CrsStatusText => $"CRS: {CrsText}";
    public string BoundsText => ActiveRasterMetadata is null
        ? "-"
        : $"{Format(ActiveRasterMetadata.Bounds.MinX)}, {Format(ActiveRasterMetadata.Bounds.MinY)} - {Format(ActiveRasterMetadata.Bounds.MaxX)}, {Format(ActiveRasterMetadata.Bounds.MaxY)}";
    public string ResolutionText => ActiveRasterMetadata is null ? "-" : $"{Format(ActiveRasterMetadata.ResolutionX)} x {Format(ActiveRasterMetadata.ResolutionY)}";
    public string NoDataText => ActiveRasterMetadata?.NoDataValue?.ToString("G") ?? "-";
    public string FormatText => ActiveRasterMetadata is not null
        ? GetFormatText(ActiveRasterMetadata)
        : "-";
    public string ZoomText => $"Zoom: {ZoomLevel * 100:0}%";
    public string LoadingStateText => $"Loading: {LoadingState}";
    public string LayerPanelWidth => IsLayerPanelOpen ? "288" : "0";
    public string MetadataPanelWidth => IsMetadataPanelOpen ? "340" : "0";
    public string LayerManagerToggleText => IsLayerPanelOpen ? "Close Layer Manager" : "Open Layer Manager";
    public string MetadataToggleText => IsMetadataPanelOpen ? "Close Metadata" : "Open Metadata";
    public bool HasSelectedLayer => SelectedLayer is not null;
    public string CoordinateText => ActiveRasterMetadata is null
        ? "Cursor: -"
        : $"Cursor: {Format(_coordinateService.PixelToWorld(ActiveRasterMetadata, MouseImagePosition).X)}, {Format(_coordinateService.PixelToWorld(ActiveRasterMetadata, MouseImagePosition).Y)}";

    partial void OnActiveRasterMetadataChanged(RasterMetadata? value)
    {
        NotifyMetadataChanged();
    }

    partial void OnSelectedLayerChanged(LayerViewModel? oldValue, LayerViewModel? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.PropertyChanged -= SelectedLayer_PropertyChanged;
        }

        if (newValue is not null)
        {
            newValue.PropertyChanged += SelectedLayer_PropertyChanged;
        }

        ActiveRasterMetadata = newValue?.Metadata;
        ActiveLayerText = newValue?.Name ?? "-";
        OnPropertyChanged(nameof(HasSelectedLayer));
        RemoveSelectedLayerCommand.NotifyCanExecuteChanged();
    }

    partial void OnMouseImagePositionChanged(GeoCoordinate value)
    {
        OnPropertyChanged(nameof(CoordinateText));
    }

    partial void OnZoomLevelChanged(double value)
    {
        OnPropertyChanged(nameof(ZoomText));
    }

    partial void OnLoadingStateChanged(string value)
    {
        OnPropertyChanged(nameof(LoadingStateText));
    }

    partial void OnIsLayerPanelOpenChanged(bool value)
    {
        OnPropertyChanged(nameof(LayerPanelWidth));
        OnPropertyChanged(nameof(LayerManagerToggleText));
    }

    partial void OnIsMetadataPanelOpenChanged(bool value)
    {
        OnPropertyChanged(nameof(MetadataPanelWidth));
        OnPropertyChanged(nameof(MetadataToggleText));
    }

    [RelayCommand]
    private async Task OpenRasterAsync()
    {
        var filePath = _fileDialogService.OpenRaster();
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        await RunSafelyAsync(async () =>
        {
            Status = "Opening raster...";
            LoadingState = "Raster metadata";
            await using var source = _rasterSourceFactory.Open(filePath);
            var metadata = await source.GetMetadataAsync();
            var tileSource = _tileSourceFactory.Create(filePath);

            AddLayer(new LayerState(Guid.NewGuid(), Path.GetFileName(filePath), filePath, LayerKind.Raster), metadata, tileSource);
            LoadingState = "Raster tiles";
            RasterLoaded?.Invoke(this, EventArgs.Empty);
            Status = $"Loaded {Path.GetFileName(filePath)}";
            LoadingState = "Idle";
        });
    }

    [RelayCommand]
    private void FitRaster()
    {
        Status = "Fit raster";
        FitRasterRequest?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ZoomIn()
    {
        Status = "Zoom in";
        ZoomRequest?.Invoke(this, 1.15);
    }

    [RelayCommand]
    private void ZoomOut()
    {
        Status = "Zoom out";
        ZoomRequest?.Invoke(this, 1 / 1.15);
    }

    [RelayCommand]
    private void ToggleLayerPanel()
    {
        IsLayerPanelOpen = !IsLayerPanelOpen;
    }

    [RelayCommand]
    private void ToggleMetadataPanel()
    {
        IsMetadataPanelOpen = !IsMetadataPanelOpen;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedLayer))]
    private void RemoveSelectedLayer()
    {
        RemoveLayer(SelectedLayer);
    }

    [RelayCommand]
    private void RemoveLayer(LayerViewModel? layer)
    {
        if (layer is null)
        {
            return;
        }

        var removedName = layer.Name;
        var removedSelectedLayer = SelectedLayer == layer;
        Layers.Remove(layer);

        if (removedSelectedLayer)
        {
            SelectedLayer = Layers.LastOrDefault();
        }

        if (Layers.Count == 0)
        {
            ActiveRasterMetadata = null;
            MouseImagePosition = new GeoCoordinate(0, 0);
            ZoomLevel = 1;
            ActiveLayerText = "-";
        }

        LoadingState = "Idle";
        Status = $"Removed {removedName}";
        OnPropertyChanged(nameof(HasSelectedLayer));
        RemoveSelectedLayerCommand.NotifyCanExecuteChanged();
    }

    public event EventHandler? RasterLoaded;
    public event EventHandler? FitRasterRequest;
    public event EventHandler<double>? ZoomRequest;

    private void AddLayer(LayerState layer, RasterMetadata metadata, ITileSource tileSource)
    {
        var vm = new LayerViewModel(layer, metadata, tileSource);
        Layers.Add(vm);
        SelectedLayer = vm;
    }

    private void SelectedLayer_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LayerViewModel.Opacity) or nameof(LayerViewModel.IsVisible))
        {
            OnPropertyChanged(nameof(Layers));
        }
    }

    private async Task RunSafelyAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Rasteria operation failed");
            Status = exception.Message;
            LoadingState = "Failed";
            System.Windows.MessageBox.Show(exception.Message, "Rasteria", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void NotifyMetadataChanged()
    {
        OnPropertyChanged(nameof(FileName));
        OnPropertyChanged(nameof(MetadataTypeText));
        OnPropertyChanged(nameof(RasterSizeText));
        OnPropertyChanged(nameof(BandCountText));
        OnPropertyChanged(nameof(CrsText));
        OnPropertyChanged(nameof(CrsStatusText));
        OnPropertyChanged(nameof(BoundsText));
        OnPropertyChanged(nameof(ResolutionText));
        OnPropertyChanged(nameof(NoDataText));
        OnPropertyChanged(nameof(FormatText));
        OnPropertyChanged(nameof(CoordinateText));
    }

    private static string Format(double value)
    {
        return value.ToString("N3");
    }

    private static string GetFormatText(RasterMetadata metadata)
    {
        if (metadata.IsCloudOptimizedGeoTiffCandidate)
        {
            return "Cloud Optimized GeoTIFF candidate";
        }

        if (metadata.IsGeoTiff)
        {
            return "GeoTIFF";
        }

        return Path.GetExtension(metadata.FilePath).Equals(".ecw", StringComparison.OrdinalIgnoreCase)
            ? "ECW"
            : "Raster";
    }
}
