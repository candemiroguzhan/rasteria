namespace Rasteria.UI.Imaging;

public readonly record struct TileKey(string SourceHash, int Level, int X, int Y);
