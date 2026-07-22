namespace Rasteria.Rendering.Abstractions;

public sealed record RenderEngineDescriptor(string Name, string Description)
{
    public static RenderEngineDescriptor WpfTiledRaster { get; } = new("WPF tiled raster", "Default MVP renderer using WPF DrawingContext and raster tile sources.");
}
