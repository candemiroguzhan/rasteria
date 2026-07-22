# Rasteria

Rasteria is a focused Windows desktop geospatial viewer built with .NET 9, WPF, MVVM, and C#.

The MVP is viewer-first: open, visualize, navigate, and inspect large geospatial datasets smoothly. It is not a QGIS clone, not a full GIS editor, and does not include AI or advanced analysis features in the first version.

## Solution Structure

- `Rasteria.Core`: domain contracts for raster layers, scene layers/camera/bounds, raster sources, tile sources, render engines, viewport/camera concepts, and viewer session state.
- `Rasteria.UI`: WPF desktop shell, MVVM view models, raster viewport host, layer tree, metadata panel, toolbar, and status bar.
- `Rasteria.Raster`: GDAL-backed raster metadata and tiled raster source implementation for GeoTIFF, COG candidates, DEM/DSM/DTM-style rasters, and other GDAL-readable raster formats.
- `Rasteria.Rendering`: HelixToolkit.Wpf.SharpDX-backed WPF scene renderer for raster tiles and sampled terrain.
- `Rasteria.Infrastructure`: application composition and cross-cutting infrastructure services.
- `Rasteria.Analysis`: placeholder module reserved for future Pro/Plus analysis features.

## MVP Scope

- Open GeoTIFF, COG candidates, DEM/DSM/DTM rasters, and other GDAL-readable rasters.
- Render large rasters as tile-based textured planes in a unified 3D scene rather than loading full images into the UI.
- Navigate with orbit, pan, zoom wheel, fit scene/layer, top-down orthographic mode, and perspective mode.
- Show a layer tree and raster metadata panel.

## Future Modules

- Pro: terrain analysis, slope, aspect, contour, profiles, and volume.
- Plus: AI-assisted raster summaries, object detection, segmentation, and report generation.

## Build and Run

```powershell
dotnet restore Rasteria.slnx
dotnet build Rasteria.slnx
dotnet run --project src/Rasteria.UI/Rasteria.UI.csproj
```
