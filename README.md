# Rasteria 🛰️

[![Stars](https://img.shields.io/github/stars/candemiroguzhan/rasteria?style=social)](https://github.com/candemiroguzhan/rasteria)
[![Forks](https://img.shields.io/github/forks/candemiroguzhan/rasteria?style=social)](https://github.com/candemiroguzhan/rasteria)
[![Issues](https://img.shields.io/github/issues/candemiroguzhan/rasteria)](https://github.com/candemiroguzhan/rasteria/issues)
[![Contributors](https://img.shields.io/github/contributors/candemiroguzhan/rasteria)](https://github.com/candemiroguzhan/rasteria/graphs/contributors)
[![Platform](https://img.shields.io/badge/Platform-Windows-0078D4?logo=windows)](https://github.com/candemiroguzhan/rasteria)
[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](./LICENSE)

Rasteria is a modern Windows desktop application for viewing, inspecting, and analyzing geospatial raster datasets.

It is designed to work efficiently with large raster files by reading spatial metadata through GDAL and rendering only the visible portions of the dataset through a tiled viewport. Rasteria provides a focused desktop environment for working with GeoTIFF, Cloud Optimized GeoTIFF, orthophotos, satellite imagery, elevation models, and other GDAL-compatible raster formats.

Built with .NET 9, WPF, MVVM, GDAL, HelixToolkit.SharpDX, Serilog, and C#.

## ✨ Features

* 🗺️ Open GDAL-compatible raster datasets
* 🛰️ View GeoTIFF, COG, orthophoto, satellite imagery, and elevation raster files
* 🧩 Render large raster datasets through an asynchronous tiled viewport
* ⚡ Load only visible raster tiles instead of the entire image
* 💾 Cache generated tiles in memory and in the system temporary directory
* 🔍 Navigate using mouse-wheel zoom, click-drag pan, toolbar controls, and fit-to-raster
* 📚 Manage multiple raster layers
* 👁️ Toggle layer visibility
* 🎚️ Control layer opacity
* 🧭 Inspect cursor position in raster pixel and world coordinates
* 📐 Read raster bounds, resolution, dimensions, CRS, band count, and no-data values
* 🧾 Inspect selected GDAL metadata domains
* 🌑 Use a modern shared dark desktop theme
* 🪟 Use custom WPF window chrome and application branding
* 📋 Log application activity and failures through rolling log files
* 🧱 Maintain a modular architecture for future 2D, terrain, and 3D raster workflows

## 🧪 Technology

* .NET 9 — Target framework
* C# — Core programming language
* WPF — Windows desktop user interface
* MVVM — Presentation architecture
* GDAL — Raster reading, metadata extraction, and geospatial processing
* MaxRev.Gdal — Native GDAL runtime distribution
* HelixToolkit.SharpDX — 3D rendering foundation
* Microsoft.Extensions.Hosting — Application hosting and lifecycle management
* Microsoft.Extensions.DependencyInjection — Dependency injection
* Serilog — Structured application logging
* xUnit — Unit testing
* Windows App SDK — Windows packaging support
* MSIX — Microsoft Store and packaged application distribution

## 📁 Project Structure

```text
Rasteria
├─ src
│  ├─ Rasteria.Core
│  │  ├─ Geometry
│  │  ├─ Layers
│  │  ├─ Raster
│  │  ├─ Rendering
│  │  ├─ Scene
│  │  ├─ Sessions
│  │  └─ Viewport
│  │
│  ├─ Rasteria.Raster
│  │  ├─ Gdal
│  │  ├─ Metadata
│  │  ├─ Sources
│  │  └─ Tiles
│  │
│  ├─ Rasteria.Rendering
│  │  ├─ Cameras
│  │  ├─ Math
│  │  ├─ Scene
│  │  └─ SharpDX
│  │
│  ├─ Rasteria.Infrastructure
│  │  ├─ DependencyInjection
│  │  ├─ Logging
│  │  └─ Services
│  │
│  └─ Rasteria.UI
│     ├─ Assets
│     ├─ Controls
│     ├─ Converters
│     ├─ Services
│     ├─ Themes
│     ├─ ViewModels
│     └─ Views
│
├─ tests
│  └─ Rasteria.Rendering.Tests
│
└─ Rasteria.slnx
```

## 🧩 Architecture

Rasteria follows a modular architecture that separates raster access, rendering, application infrastructure, and desktop presentation concerns.

### Rasteria.Core

Contains the primary contracts and domain models used throughout the application.

Core responsibilities include:

* Raster metadata models
* Raster source abstractions
* Tile source contracts
* Geometry and bounding-box models
* Layer definitions
* Scene state
* Camera state
* Viewer session state
* Rendering-independent application contracts

### Rasteria.Raster

Provides the GDAL-backed raster implementation.

Responsibilities include:

* Opening GDAL-compatible raster datasets
* Reading spatial metadata
* Extracting coordinate reference system information
* Calculating raster bounds and resolution
* Reading raster bands
* Creating visible raster tiles
* Handling no-data values
* Accessing GDAL metadata domains

### Rasteria.Rendering

Contains rendering-related abstractions and mathematical utilities.

Responsibilities include:

* Raster-to-scene coordinate calculations
* Camera and viewport mathematics
* Scene composition
* HelixToolkit.SharpDX integration
* Foundation for terrain and 3D raster rendering

### Rasteria.Infrastructure

Contains application-level infrastructure and dependency composition.

Responsibilities include:

* Dependency injection registration
* Application service composition
* Logging configuration
* Shared infrastructure services

### Rasteria.UI

Contains the WPF desktop application.

Responsibilities include:

* Application shell
* MVVM view models
* Raster viewport interaction
* Layer management
* Metadata panels
* File dialogs
* Shared themes
* Application commands
* Tile caching integration
* Window chrome and desktop behavior

## 📋 Requirements

* Windows 10 or Windows 11
* Windows x64 architecture
* .NET 9 SDK for development
* Git
* JetBrains Rider, Visual Studio, or another compatible .NET IDE

The main UI project targets:

```text
net9.0-windows10.0.26100.0
```

The application is configured as a self-contained `win-x64` executable.

## ⚙️ Installation

Clone the repository:

```bash
git clone https://github.com/candemiroguzhan/rasteria.git
cd rasteria
```

Restore dependencies:

```powershell
dotnet restore Rasteria.slnx
```

Build the solution:

```powershell
dotnet build Rasteria.slnx
```

Run tests:

```powershell
dotnet test Rasteria.slnx
```

Run the desktop application:

```powershell
dotnet run --project src/Rasteria.UI/Rasteria.UI.csproj
```

## ▶️ Usage

1. Start Rasteria.
2. Select a GDAL-compatible raster file.
3. Wait for the raster metadata and initial visible tiles to load.
4. Navigate through the dataset using mouse and toolbar controls.
5. Inspect spatial metadata, layer properties, and cursor coordinates.
6. Add additional raster layers when required.
7. Control layer visibility and opacity through the layer panel.

### Viewport Controls

| Action              | Control                         |
| ------------------- | ------------------------------- |
| Zoom in or out      | Mouse wheel                     |
| Pan                 | Left mouse button and drag      |
| Zoom in             | Toolbar zoom-in button          |
| Zoom out            | Toolbar zoom-out button         |
| Fit raster          | Fit-to-raster command           |
| Inspect coordinates | Move the cursor over the raster |

## 🗺️ Supported Raster Data

Rasteria can open raster formats supported by the installed GDAL runtime.

Common examples include:

* GeoTIFF
* Cloud Optimized GeoTIFF
* TIFF
* JPEG
* PNG
* ECW, depending on GDAL driver availability
* JPEG 2000, depending on GDAL driver availability
* Digital Elevation Models
* Orthophotos
* Satellite imagery
* Multiband raster datasets

Available formats may vary depending on the GDAL drivers included in the runtime distribution.

## 📐 Raster Metadata

Rasteria can inspect and display information such as:

* File name and source path
* Raster width and height
* Raster band count
* Coordinate reference system
* Spatial reference authority
* GeoTransform values
* Raster bounds
* Pixel resolution
* No-data value
* Data type
* Band metadata
* GDAL driver information
* Selected GDAL metadata domains
* Cursor pixel coordinates
* Cursor world coordinates

## 🧱 Tiled Rendering

Rasteria does not load the complete source raster into a single WPF image.

Instead, the viewport:

1. Determines the currently visible raster region.
2. Calculates the required tile coordinates.
3. Requests only the necessary raster tiles.
4. Loads tiles asynchronously through the raster source.
5. Caches rendered tiles.
6. Reuses cached tiles while the source file revision remains valid.

This approach reduces memory pressure and provides a scalable foundation for working with large geospatial imagery.

## 💾 Tile Caching

`CachedTileSource` wraps raster tile loading with two cache layers:

* In-memory cache for frequently accessed tiles
* Disk cache under the system temporary directory

Cache entries are associated with the active source file revision so stale tiles can be invalidated when the underlying dataset changes.

Temporary tile data is intended for runtime acceleration and should not be treated as permanent project data.

## ⚙️ Development Notes

### Application Startup

`App.xaml.cs` manages:

* Application host creation
* Dependency injection
* Serilog initialization
* Main window creation
* Application startup and shutdown lifecycle

### Theme System

`App.xaml` loads the shared theme from:

```text
src/Rasteria.UI/Themes/DarkTheme.xaml
```

Application-wide colors, brushes, typography, and control styles should be placed in the shared theme unless a view requires local styling.

### Main Window View Model

`MainWindowViewModel` coordinates:

* Opening raster files
* Managing raster layers
* Updating selected-layer state
* Displaying raster metadata
* Controlling application panels
* Executing viewport commands
* Updating coordinate information

### Raster Viewport

`MapViewer` is responsible for:

* Displaying raster layers
* Calculating visible tiles
* Loading tiles asynchronously
* Handling zoom and pan
* Updating cursor coordinates
* Applying layer visibility
* Applying layer opacity
* Fitting the raster to the viewport

### Logging

Rasteria writes rolling application log files under:

```text
logs/
```

Logs may include:

* Application startup information
* Raster opening activity
* GDAL failures
* Tile-loading errors
* Rendering exceptions
* Unexpected application failures

## 🛣️ Roadmap

Planned development areas include:

### Raster Visualization

* Per-band visualization
* RGB band composition
* Contrast and brightness controls
* Histogram visualization
* Color ramps
* No-data visualization
* Raster transparency controls
* Overview and pyramid support


## 🤝 Contributing

Contributions, bug reports, and feature suggestions are welcome.

You can contribute by:

* Opening issues for bugs or feature requests
* Submitting pull requests
* Improving documentation
* Adding raster format tests
* Improving GDAL integration
* Extending raster analysis modules
* Adding rendering and viewport tests

When contributing, keep platform-specific concerns inside the UI or infrastructure layers and avoid introducing WPF, GDAL, or SharpDX dependencies into the core domain abstractions.

## 👤 Author

**Oğuzhan CANDEMİR**
Geospatial Software Engineer

GitHub: `@candemiroguzhan`

## 📄 License

This project is licensed under the MIT License.

See the [`LICENSE`](./LICENSE) file for details.

## ⭐ Support

If you find Rasteria useful:

* Star the repository ⭐
* Report bugs
* Suggest new raster workflows
* Improve the documentation
* Contribute enhancements

Thank you for your support.
