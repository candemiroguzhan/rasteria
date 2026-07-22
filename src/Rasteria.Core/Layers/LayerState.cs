namespace Rasteria.Core.Layers;

public sealed record LayerState(Guid Id, string Name, string SourcePath, LayerKind Kind, bool IsVisible = true, double Opacity = 1);
