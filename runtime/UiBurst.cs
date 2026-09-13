using Godot;
using System;

namespace MaxGodotUI;

/// <summary>Deterministic local geometry; the caller reserves its particle budget with the timeline.</summary>
[GlobalClass]
public partial class UiBurst : Control
{
    public string Shape { get; set; } = "rays";
    public long Seed { get; set; }
    public int Particles { get; set; }
    public float Progress { get; set; }
    public Color Ink { get; set; } = Colors.White;
    public Label? FloatingLabel { get; set; }
    public Vector2 LabelOrigin { get; set; }
    private ShaderMaterial? ripple;
    public void EnableRipple(bool fractured = false)
    {
        if (ripple != null) return;
        ripple = UiShaders.Material("ripple"); ripple.SetShaderParameter("ink", Ink);
        ripple.SetShaderParameter("fractured", fractured ? 1f : 0f);
        AddChild(new ColorRect { Size = Size, Material = ripple, MouseFilter = MouseFilterEnum.Ignore });
    }
    public void Sample(float progress, bool detailed = true)
    {
        Progress = detailed ? progress : .35f;
        ripple?.SetShaderParameter("progress", progress);
        QueueRedraw(); Modulate = new(1, 1, 1, 1 - progress * progress);
        if (FloatingLabel != null && detailed)
        {
            FloatingLabel.Position = LabelOrigin - new Vector2(0, 24 * UiTimeline.Ease(progress));
            FloatingLabel.PivotOffset = FloatingLabel.Size / 2;
            FloatingLabel.Scale = Vector2.One * (1 + .12f * MathF.Sin(Math.Min(1, progress * 3) * Mathf.Pi));
        }
    }
    public override void _Draw()
    {
        Vector2 c = Size / 2;
        float radiusScale = Math.Min(Size.X, Size.Y) / 180f;
        float r = (16 + 66 * UiTimeline.Ease(Progress)) * radiusScale;
        if (Shape == "cross") { DrawLine(c + new Vector2(-r / 2, 0), c + new Vector2(r / 2, 0), Ink, 4); DrawLine(c + new Vector2(0, -r / 2), c + new Vector2(0, r / 2), Ink, 4); }
        else if (Shape == "rays")
            for (int i = 0; i < 4; i++) { Vector2 n = Vector2.Right.Rotated(Mathf.Pi / 4 + i * Mathf.Pi / 2); DrawLine(c + n * r * .65f, c + n * r, Ink, 3, true); }
        else if (Shape == "segments")
            for (int i = 0; i < 6; i++) DrawArc(c, r, i * Mathf.Tau / 6, (i + .65f) * Mathf.Tau / 6, 8, Ink, 3, true);
        else DrawArc(c, r, 0, Mathf.Tau, 32, Ink, 2, true);
        for (int i = 0; i < Particles; i++)
        {
            float angle = i * 2.39996f + (Seed % 97) * .17f;
            Vector2 n = Vector2.Right.Rotated(angle), point = c + n * r * (1 + i % 3 * .12f);
            DrawLine(point, point + n * (3 + i % 4), new Color(Ink, 1 - Progress), 2, true);
        }
    }
}
