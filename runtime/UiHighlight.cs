using Godot;
using System;

namespace MaxGodotUI;

/// <summary>An input-transparent outline following a source's final canvas transform.</summary>
[GlobalClass]
public partial class UiHighlight : Control
{
    [Export] public Control? Source { get; set; }
    /// <summary>Empty uses the source's size. Coordinates are local to Source.</summary>
    [Export] public Rect2 SourceRect { get; set; }
    [Export] public float Radius { get; set; } = 4;
    public HighlightStyle? CurrentStyle { get; private set; }
    public int Detail { get; private set; } = 2;
    public bool IsTransitioning => elapsed < duration;
    public bool IsPulsing => pulseRemaining > 0;
    public int ParameterUpdates { get; private set; }
    public Transform2D SourceTransform => IsInstanceValid(Source) && Source!.IsInsideTree()
        ? GetGlobalTransformWithCanvas().AffineInverse() * Source.GetGlobalTransformWithCanvas()
        : Transform2D.Identity;

    private readonly record struct Look(Color Ink, float Line, float Glow, float Opacity, float Corners, float Breath, float Period)
    {
        public static Look From(HighlightStyle? style) => style == null ? default :
            new(style.Ink, Math.Clamp(style.LineWidth, 0, 8), Math.Clamp(style.GlowWidth, 0, 32),
                Math.Clamp(style.GlowOpacity, 0, 1), Math.Max(0, style.CornerLength),
                Math.Clamp(style.BreathAmount, 0, .1f), Math.Max(.1f, style.BreathPeriod));
        public Look Blend(Look other, float t) => new(Ink.Lerp(other.Ink, t),
            Mathf.Lerp(Line, other.Line, t), Mathf.Lerp(Glow, other.Glow, t),
            Mathf.Lerp(Opacity, other.Opacity, t), Mathf.Lerp(Corners, other.Corners, t),
            Mathf.Lerp(Breath, other.Breath, t), Mathf.Lerp(Period, other.Period, t));
    }
    private ShaderMaterial? shaderMaterial;
    private Look start, current, destination;
    private double elapsed, duration, pulseRemaining, clock;
    private bool parametersDirty;
    private Rect2 drawnRect;
    private float drawnRadius = -1;

    public UiHighlight() { MouseFilter = MouseFilterEnum.Ignore; Visible = false; }

    public void SetStyle(HighlightStyle? style, double seconds = .14)
    {
        if (CurrentStyle == style) return;
        CurrentStyle = style;
        if (!IsPulsing) Begin(style, seconds);
    }

    /// <summary>A single temporary override; repeated calls replace rather than stack.</summary>
    public void Pulse(HighlightStyle style, double seconds = .16)
    {
        if (seconds <= 0) { pulseRemaining = 0; Begin(CurrentStyle, 0); return; }
        pulseRemaining = Math.Max(0, seconds);
        Begin(style, 0);
    }

    private void Begin(HighlightStyle? style, double seconds)
    {
        start = current; destination = Look.From(style);
        // Fade opacity without changing the outgoing geometry's shape.
        if (style == null) destination = current with { Ink = new Color(current.Ink, 0), Breath = 0 };
        if (current.Ink.A == 0 && style != null) start = destination with { Ink = new Color(destination.Ink, 0) };
        duration = Detail == 0 ? 0 : Math.Max(0, seconds); elapsed = 0;
        if (duration == 0) current = destination;
        parametersDirty = true;
        Visible = current.Ink.A > 0 || destination.Ink.A > 0;
        QueueRedraw();
    }

    public void SetDetail(int detail)
    {
        detail = Math.Clamp(detail, 0, 2);
        if (Detail == detail) return;
        Detail = detail; parametersDirty = true;
        if (detail == 0) { elapsed = duration; current = destination; }
        ApplyParameters(); QueueRedraw();
    }

    /// <summary>The host supplies unscaled seconds; zero delta freezes transitions and pulses.</summary>
    public void UpdateSurface(double time, double delta)
    {
        if (!IsInstanceValid(Source) || !Source!.IsInsideTree())
        {
            Hide(); ReleaseMaterial(); return;
        }
        double step = Math.Max(0, delta);
        if (IsPulsing && (pulseRemaining -= step) <= 0)
        { Begin(CurrentStyle, 0); }
        if (IsTransitioning)
        {
            elapsed = Math.Min(duration, elapsed + step);
            float t = (float)(elapsed / duration); t = t * t * (3 - 2 * t);
            current = start.Blend(destination, t); parametersDirty = true;
        }
        Visible = Source.IsVisibleInTree() && (current.Ink.A > .001f || IsTransitioning);
        if (!Visible) return;
        if (Detail == 2 && current.Breath > 0 && clock != time) { clock = time; parametersDirty = true; }
        ApplyParameters();
        // Resolve at draw time, after the host's layout tweens and visual springs.
        QueueRedraw();
    }

    private void ApplyParameters()
    {
        if (!Visible || !parametersDirty) return;
        shaderMaterial ??= UiShaders.Material("highlight_outline"); Material = shaderMaterial;
        shaderMaterial.SetShaderParameter("ink", current.Ink);
        shaderMaterial.SetShaderParameter("line_width", current.Line);
        shaderMaterial.SetShaderParameter("glow_width", Detail == 0 ? 0 : Detail == 1 ? Math.Min(3, current.Glow) : current.Glow);
        shaderMaterial.SetShaderParameter("glow_opacity", Detail == 1 ? current.Opacity * .55f : current.Opacity);
        shaderMaterial.SetShaderParameter("corner_length", current.Corners);
        shaderMaterial.SetShaderParameter("brightness", Detail == 2 && current.Breath > 0
            ? 1f - current.Breath * .5f + current.Breath * .5f * MathF.Sin((float)(clock * Math.Tau / Math.Max(.1, current.Period))) : 1f);
        parametersDirty = false; ParameterUpdates++;
    }

    public override void _Draw()
    {
        if (!IsInstanceValid(Source) || !Source!.IsVisibleInTree() || shaderMaterial == null) return;
        Rect2 rect = SourceRect.Size == Vector2.Zero ? new(Vector2.Zero, Source.Size) : SourceRect;
        if (drawnRect != rect || drawnRadius != Radius)
        {
            shaderMaterial.SetShaderParameter("rect_origin", rect.Position);
            shaderMaterial.SetShaderParameter("rect_size", rect.Size);
            shaderMaterial.SetShaderParameter("radius", Radius);
            drawnRect = rect; drawnRadius = Radius;
        }
        DrawSetTransformMatrix(SourceTransform);
        // An external decoration shares only part of the source's ancestor chain.
        // Carry the missing modulation across so a departing source also fades its outline.
        Color tint = Colors.White;
        for (Node? node = Source; node != null && !node.IsAncestorOf(this); node = node.GetParent())
            if (node is CanvasItem item) tint *= item.Modulate;
        DrawRect(rect.Grow(Math.Max(4, current.Glow + current.Line + 1)), tint);
    }

    private void ReleaseMaterial() { Material = null; shaderMaterial?.Dispose(); shaderMaterial = null; parametersDirty = true; drawnRadius = -1; }
    // Release native resources while the tree is alive. Look/elapsed survive reparenting;
    // the next host update restores the material parameters without replaying the transition.
    public override void _ExitTree() => ReleaseMaterial();
}
