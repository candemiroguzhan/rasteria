namespace Rasteria.Core.Rasters;

public interface ITileSource
{
    Task<RasterMetadata> GetMetadataAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TileCoordinate>> GetVisibleTilesAsync(ViewportState viewport, CancellationToken cancellationToken = default);
    Task<RasterTile> GetTileAsync(TileCoordinate coordinate, CancellationToken cancellationToken = default);
}
