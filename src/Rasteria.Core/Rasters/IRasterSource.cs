namespace Rasteria.Core.Rasters;

public interface IRasterSource : IAsyncDisposable
{
    string FilePath { get; }
    Task<RasterMetadata> GetMetadataAsync(CancellationToken cancellationToken = default);
}
