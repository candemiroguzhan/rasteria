namespace Rasteria.Core.Rasters;

public sealed record RasterBandInfo(int Index, string DataType, string ColorInterpretation, double? NoDataValue);
