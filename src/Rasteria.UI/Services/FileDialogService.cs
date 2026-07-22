using Microsoft.Win32;

namespace Rasteria.UI.Services;

public sealed class FileDialogService : IFileDialogService
{
    public string? OpenRaster()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open raster",
            Filter = "Raster files|*.tif;*.tiff;*.geotiff;*.dem;*.img;*.vrt|GeoTIFF|*.tif;*.tiff;*.geotiff|ECW|*.ecw|All files|*.*",
            CheckFileExists = true
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
