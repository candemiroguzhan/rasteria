namespace Rasteria.Core.Rasters;

public sealed record RasterTile(
    TileCoordinate Coordinate,
    int SourceX,
    int SourceY,
    int SourceWidth,
    int SourceHeight,
    int PixelWidth,
    int PixelHeight,
    int Stride,
    byte[] Bgra32Pixels);
