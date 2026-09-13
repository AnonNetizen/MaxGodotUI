using Godot;

namespace MaxGodotUI;

/// <summary>Immutable-by-convention outline parameters; no interaction semantics.</summary>
[GlobalClass]
public partial class HighlightStyle : Resource
{
    [Export] public Color Ink { get; set; } = Colors.White;
    [Export] public float LineWidth { get; set; } = 2;
    [Export] public float GlowWidth { get; set; } = 8;
    [Export] public float GlowOpacity { get; set; } = .3f;
    [Export] public float CornerLength { get; set; }
    [Export] public float BreathAmount { get; set; }
    [Export] public float BreathPeriod { get; set; } = 2.4f;
}
