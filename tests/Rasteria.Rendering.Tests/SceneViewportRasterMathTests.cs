using Rasteria.Core.Geometry;
using Rasteria.Core.Rasters;
using Rasteria.Core.Scene;
using Rasteria.Rendering.Scene;
using SharpDX;
using Xunit;

namespace Rasteria.Rendering.Tests;

public sealed class SceneViewportRasterMathTests
{
    [Fact]
    public void ToLocalBounds_SubtractsSharedOrigin()
    {
        var bounds = new SceneBounds(450000, 4400000, 20, 450100, 4400200, 30);

        var local = SceneViewportRasterMath.ToLocalBounds(bounds, 450050, 4400100, 25);

        Assert.Equal(new SceneBounds(-50, -100, -5, 50, 100, 5), local);
    }

    [Fact]
    public void TryCalculateTileSourceRect_UsesLongMathAndClampsToRaster()
    {
        var metadata = CreateMetadata(width: 2000, height: 1500, tileSize: 512);
        var coordinate = new TileCoordinate(0, 0, 2);

        var ok = SceneViewportRasterMath.TryCalculateTileSourceRect(metadata, coordinate, out var rect);

        Assert.True(ok);
        Assert.Equal(new RasterTileSourceRect(0, 0, 2000, 1500), rect);
    }

    [Fact]
    public void TryCalculateTileSourceRect_ReturnsFalseForOutsideTile()
    {
        var metadata = CreateMetadata(width: 2000, height: 1500, tileSize: 512);

        var ok = SceneViewportRasterMath.TryCalculateTileSourceRect(metadata, new TileCoordinate(100, 0, 30), out _);

        Assert.False(ok);
    }

    [Fact]
    public void ValidateTileBuffer_RejectsInvalidLength()
    {
        var tile = new RasterTile(new TileCoordinate(0, 0, 0), 0, 0, 10, 10, 2, 2, 8, new byte[15]);

        var exception = Assert.Throws<InvalidDataException>(() => SceneViewportRasterMath.ValidateTileBuffer(tile));

        Assert.Contains("invalid length", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateTileBuffer_AcceptsExpectedBgraLength()
    {
        var tile = new RasterTile(new TileCoordinate(0, 0, 0), 0, 0, 10, 10, 2, 2, 8, new byte[16]);

        SceneViewportRasterMath.ValidateTileBuffer(tile);
    }

    [Fact]
    public void WithAlpha_PreservesRgbAndClampsAlpha()
    {
        var color = new Color4(0.2f, 0.4f, 0.6f, 0.8f);

        var result = SceneViewportRasterMath.WithAlpha(color, 2);

        Assert.Equal(color.Red, result.Red);
        Assert.Equal(color.Green, result.Green);
        Assert.Equal(color.Blue, result.Blue);
        Assert.Equal(1f, result.Alpha);
    }

    [Fact]
    public void SanitizeBounds_UsesDefaultForEmptyOrNonFiniteBounds()
    {
        Assert.Equal(SceneViewportRasterMath.DefaultBounds, SceneViewportRasterMath.SanitizeBounds(SceneBounds.Empty));
        Assert.Equal(SceneViewportRasterMath.DefaultBounds, SceneViewportRasterMath.SanitizeBounds(new SceneBounds(double.NaN, 0, 0, 1, 1, 1)));
    }

    [Fact]
    public void CalculateOrthographicWidth_UsesPositiveFallbackForEmptyBounds()
    {
        var width = SceneViewportRasterMath.CalculateOrthographicWidth(SceneBounds.Empty);

        Assert.True(width >= 1);
    }

    [Fact]
    public void GetWorldCorners_UsesAllFourCornersForRotatedRaster()
    {
        var transform = new GeoTransform(100, 2, 0.5, 200, 0.25, -2);

        var corners = SceneViewportRasterMath.GetWorldCorners(transform, 10, 20, 30, 40);

        Assert.Equal(transform.PixelToWorld(10, 20), corners.TopLeft);
        Assert.Equal(transform.PixelToWorld(40, 20), corners.TopRight);
        Assert.Equal(transform.PixelToWorld(40, 60), corners.BottomRight);
        Assert.Equal(transform.PixelToWorld(10, 60), corners.BottomLeft);
        Assert.NotEqual(corners.TopLeft.X, corners.BottomLeft.X);
        Assert.NotEqual(corners.TopRight.Y, corners.BottomRight.Y);
    }

    [Fact]
    public void BoundsFromLocalCorners_CalculatesFiniteBounds()
    {
        var bounds = SceneViewportRasterMath.BoundsFromLocalCorners(
            new Vector3(-1, 3, 0),
            new Vector3(4, 2, 0),
            new Vector3(2, -5, 0),
            new Vector3(-2, -4, 0));

        Assert.Equal(new SceneBounds(-2, -5, 0, 4, 3, 0), bounds);
    }

    private static RasterMetadata CreateMetadata(int width, int height, int tileSize)
    {
        return new RasterMetadata(
            "test.tif",
            width,
            height,
            3,
            tileSize,
            true,
            false,
            "Test",
            new CoordinateSystemInfo("Test", "EPSG", 4326, null),
            GeoTransform.Identity,
            new GeoBounds(0, 0, width, height),
            1,
            1,
            [new RasterBandInfo(1, "Byte", "Red", null)],
            new Dictionary<string, string>());
    }
}
