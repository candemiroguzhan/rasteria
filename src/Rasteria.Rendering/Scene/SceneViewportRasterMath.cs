using Rasteria.Core.Geometry;
using Rasteria.Core.Rasters;
using Rasteria.Core.Scene;
using SharpDX;
using System.IO;

namespace Rasteria.Rendering.Scene;

internal readonly record struct RasterTileSourceRect(
    int SourceX,
    int SourceY,
    int SourceWidth,
    int SourceHeight);

internal readonly record struct RasterQuadWorldCorners(
    GeoCoordinate TopLeft,
    GeoCoordinate TopRight,
    GeoCoordinate BottomRight,
    GeoCoordinate BottomLeft);

internal static class SceneViewportRasterMath
{
    public static SceneBounds DefaultBounds { get; } = new(-50, -50, 0, 50, 50, 0);

    public static SceneBounds ToLocalBounds(SceneBounds bounds, double originX, double originY, double originZ)
    {
        return new SceneBounds(
            bounds.MinX - originX,
            bounds.MinY - originY,
            bounds.MinZ - originZ,
            bounds.MaxX - originX,
            bounds.MaxY - originY,
            bounds.MaxZ - originZ);
    }

    public static bool TryCalculateTileSourceRect(
        RasterMetadata metadata,
        TileCoordinate coordinate,
        out RasterTileSourceRect rect)
    {
        rect = default;
        if (metadata.Width <= 0 || metadata.Height <= 0 || metadata.TileSize <= 0)
        {
            return false;
        }

        var level = Math.Clamp(coordinate.Level, 0, 30);
        var levelScale = 1L << level;
        var sourceTileSize = (long)metadata.TileSize * levelScale;
        if (sourceTileSize <= 0)
        {
            return false;
        }

        var sourceX = (long)coordinate.X * sourceTileSize;
        var sourceY = (long)coordinate.Y * sourceTileSize;
        if (sourceX < 0 || sourceY < 0 || sourceX >= metadata.Width || sourceY >= metadata.Height)
        {
            return false;
        }

        var sourceWidth = Math.Min(sourceTileSize, (long)metadata.Width - sourceX);
        var sourceHeight = Math.Min(sourceTileSize, (long)metadata.Height - sourceY);
        if (sourceWidth <= 0 || sourceHeight <= 0 || sourceWidth > int.MaxValue || sourceHeight > int.MaxValue)
        {
            return false;
        }

        rect = new RasterTileSourceRect((int)sourceX, (int)sourceY, (int)sourceWidth, (int)sourceHeight);
        return true;
    }

    public static void ValidateTileBuffer(RasterTile tile)
    {
        if (tile.PixelWidth <= 0)
        {
            throw new InvalidDataException($"Raster tile pixel width must be positive. Coordinate {tile.Coordinate}, width {tile.PixelWidth}.");
        }

        if (tile.PixelHeight <= 0)
        {
            throw new InvalidDataException($"Raster tile pixel height must be positive. Coordinate {tile.Coordinate}, height {tile.PixelHeight}.");
        }

        if (tile.Bgra32Pixels is null)
        {
            throw new InvalidDataException($"Raster tile pixel buffer is null. Coordinate {tile.Coordinate}.");
        }

        int expectedLength;
        try
        {
            expectedLength = checked(tile.PixelWidth * tile.PixelHeight * 4);
        }
        catch (OverflowException exception)
        {
            throw new InvalidDataException($"Raster tile pixel buffer length overflow. Coordinate {tile.Coordinate}, size {tile.PixelWidth}x{tile.PixelHeight}.", exception);
        }

        if (tile.Bgra32Pixels.Length != expectedLength)
        {
            throw new InvalidDataException($"Raster tile pixel buffer has invalid length. Coordinate {tile.Coordinate}, expected {expectedLength}, actual {tile.Bgra32Pixels.Length}.");
        }
    }

    public static Color4 WithAlpha(Color4 color, double opacity)
    {
        return new Color4(color.Red, color.Green, color.Blue, (float)Math.Clamp(opacity, 0, 1));
    }

    public static SceneBounds SanitizeBounds(SceneBounds bounds)
    {
        if (!IsFinite(bounds.MinX)
            || !IsFinite(bounds.MinY)
            || !IsFinite(bounds.MinZ)
            || !IsFinite(bounds.MaxX)
            || !IsFinite(bounds.MaxY)
            || !IsFinite(bounds.MaxZ)
            || !IsFinite(bounds.Width)
            || !IsFinite(bounds.Depth)
            || !IsFinite(bounds.Height)
            || !IsFinite(bounds.Radius)
            || bounds.Width < 0
            || bounds.Depth < 0
            || bounds.Height < 0
            || (bounds.Width == 0 && bounds.Depth == 0 && bounds.Height == 0))
        {
            return DefaultBounds;
        }

        return bounds;
    }

    public static double CalculateOrthographicWidth(SceneBounds bounds)
    {
        var safeBounds = SanitizeBounds(bounds);
        var width = Math.Max(safeBounds.Width, safeBounds.Depth) * 1.15 + 1;
        return IsFinite(width) ? Math.Max(1, width) : 116;
    }

    public static RasterQuadWorldCorners GetTileWorldCorners(RasterMetadata metadata, RasterTile tile)
    {
        return GetWorldCorners(
            metadata.GeoTransform,
            tile.SourceX,
            tile.SourceY,
            tile.SourceWidth,
            tile.SourceHeight);
    }

    public static RasterQuadWorldCorners GetWorldCorners(
        GeoTransform transform,
        double sourceX,
        double sourceY,
        double sourceWidth,
        double sourceHeight)
    {
        var topLeft = transform.PixelToWorld(sourceX, sourceY);
        var topRight = transform.PixelToWorld(sourceX + sourceWidth, sourceY);
        var bottomRight = transform.PixelToWorld(sourceX + sourceWidth, sourceY + sourceHeight);
        var bottomLeft = transform.PixelToWorld(sourceX, sourceY + sourceHeight);
        return new RasterQuadWorldCorners(topLeft, topRight, bottomRight, bottomLeft);
    }

    public static SceneBounds BoundsFromLocalCorners(params Vector3[] corners)
    {
        if (corners.Length == 0)
        {
            return SceneBounds.Empty;
        }

        return new SceneBounds(
            corners.Min(c => c.X),
            corners.Min(c => c.Y),
            corners.Min(c => c.Z),
            corners.Max(c => c.X),
            corners.Max(c => c.Y),
            corners.Max(c => c.Z));
    }

    private static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
