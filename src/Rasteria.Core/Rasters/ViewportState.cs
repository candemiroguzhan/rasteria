namespace Rasteria.Core.Rasters;

public sealed record ViewportState(
    double ImageX,
    double ImageY,
    double ImageWidth,
    double ImageHeight,
    double Scale,
    int ViewportPixelWidth,
    int ViewportPixelHeight);
