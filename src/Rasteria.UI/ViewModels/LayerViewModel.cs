using CommunityToolkit.Mvvm.ComponentModel;
using Rasteria.Core.Layers;
using Rasteria.Core.Rasters;

namespace Rasteria.UI.ViewModels;

public partial class LayerViewModel : ObservableObject
{
    public LayerViewModel(LayerState layer)
    {
        Id = layer.Id;
        Name = layer.Name;
        SourcePath = layer.SourcePath;
        Kind = layer.Kind.ToString();
        _isVisible = layer.IsVisible;
        _opacity = layer.Opacity;
    }

    public LayerViewModel(LayerState layer, RasterMetadata metadata, ITileSource tileSource)
        : this(layer)
    {
        Metadata = metadata;
        TileSource = tileSource;
    }

    public LayerViewModel(IGeoLayer layer)
    {
        Id = layer.Id;
        Name = layer.Name;
        SourcePath = layer.SourcePath;
        Kind = layer.Type.ToString();
        _isVisible = layer.IsVisible;
        _opacity = layer.Opacity;
    }

    public Guid Id { get; }
    public string Name { get; }
    public string SourcePath { get; }
    public string Kind { get; }
    public RasterMetadata? Metadata { get; }
    public ITileSource? TileSource { get; }

    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private double _opacity;
}
