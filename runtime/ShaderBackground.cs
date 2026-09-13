using Godot;
using System;

namespace MaxGodotUI;

/// <summary>Two reusable drawing surfaces, advanced only by the caller's clock.</summary>
[GlobalClass]
public partial class ShaderBackground : Control
{
    private ColorRect front = Surface("Front"), back = Surface("Back");
    private double elapsed, duration;
    private double clock;
    private Vector2 pointer, pixels = Vector2.One;
    public BackgroundStyle? CurrentStyle { get; private set; }
    public int Detail { get; private set; } = 2;
    public int ActiveLayers => (front.Visible ? 1 : 0) + (back.Visible ? 1 : 0);
    public bool IsTransitioning => back.Visible;
    public float TransitionProgress => IsTransitioning ? (float)Math.Clamp(elapsed / duration, 0, 1) : 1;

    private static ColorRect Surface(string name) => new() { Name = name, Visible = false, MouseFilter = MouseFilterEnum.Ignore };
    public ShaderBackground()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        AddChild(back); AddChild(front);
        Resized += ResizeSurfaces;
    }
    private void ResizeSurfaces() { front.Size = Size; back.Size = Size; }
    public void SetStyle(BackgroundStyle style, double transitionSeconds = .18)
    {
        if (CurrentStyle == style) return;
        // Create first so an invalid replacement cannot disturb the current view.
        var material = style.CreateMaterial();
        if (IsTransitioning && TransitionProgress < .5f) (front, back) = (back, front);
        FinishTransition();
        bool hadStyle = CurrentStyle != null;
        (front, back) = (back, front);
        front.Material = material; front.Visible = true;
        CurrentStyle = style;
        elapsed = 0; duration = Math.Max(.001, transitionSeconds);
        MoveChild(front, -1); ResizeSurfaces();
        if (!hadStyle || Detail < 2 || transitionSeconds <= 0) FinishTransition();
        else { back.Visible = true; front.Modulate = new(1, 1, 1, 0); }
        ApplyUniforms();
    }
    public void SetDetail(int value)
    {
        Detail = Math.Clamp(value, 0, 2);
        if (Detail < 2) FinishTransition();
        ApplyUniforms();
    }
    public void UpdateSurface(double time, double delta, Vector2 normalizedPointer, Vector2 displaySize)
    {
        clock = time; pointer = normalizedPointer.Clamp(-Vector2.One, Vector2.One);
        pixels = displaySize.Max(Vector2.One);
        if (IsTransitioning && delta > 0)
        {
            elapsed += delta;
            float p = TransitionProgress;
            front.Modulate = new(1, 1, 1, p * p * (3 - 2 * p));
            if (p >= 1) FinishTransition();
        }
        ApplyUniforms();
    }
    private void ApplyUniforms()
    {
        foreach (var layer in new[] { front, back })
        {
            if (!layer.Visible || layer.Material is not ShaderMaterial material) continue;
            material.SetShaderParameter("clock", clock);
            material.SetShaderParameter("detail", Detail);
            material.SetShaderParameter("pointer", Detail == 2 ? pointer : Vector2.Zero);
            material.SetShaderParameter("display_size", pixels);
        }
    }
    public void FinishTransition()
    {
        front.Modulate = Colors.White;
        back.Visible = false; ReleaseMaterial(back);
        elapsed = duration;
    }
    private static void ReleaseMaterial(ColorRect surface)
    {
        var material = surface.Material;
        surface.Material = null;
        material?.Dispose();
    }
    public override void _ExitTree()
    {
        ReleaseMaterial(front); ReleaseMaterial(back);
        front.Visible = back.Visible = false; CurrentStyle = null;
    }
}
