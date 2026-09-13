using Godot;
using System;

namespace MaxGodotUI;

/// <summary>Curve rendering only; validation and input remain with the caller.</summary>
[GlobalClass]
public partial class UiArrow : Control
{
    private Control? source;
    // Tweens advance after node processing. Resolve the anchor at draw time so it never trails
    // a moving source by one frame; the pointer and legality can still update through normal input.
    public Vector2 Origin => IsInstanceValid(source)
        ? GetGlobalTransformWithCanvas().AffineInverse() * source!.GetGlobalTransformWithCanvas() * AnchorPoint
        : Vector2.Zero;
    public Vector2 Tip { get; private set; }
    public Vector2 AnchorPoint { get; private set; }
    public Color Ink { get; private set; } = Colors.White;

    public UiArrow()
    {
        ZIndex = 80;
        MouseFilter = MouseFilterEnum.Ignore; Visible = false;
    }

    public void PointFrom(Control visual, Vector2 anchor, Vector2 tip, Color color)
    {
        source = visual; AnchorPoint = anchor; Tip = tip; Ink = color;
        Visible = new Rect2(Vector2.Zero, Size).HasPoint(tip);
        QueueRedraw();
    }

    public override void _Draw()
    {
        Vector2 origin = Origin;
        if (origin.DistanceTo(Tip) < 24) return;
        // Leave the source vertically, then bend toward the pointer without snapping its tip.
        float bend = Math.Min(180, origin.DistanceTo(Tip) * .35f);
        Vector2 control = origin + Vector2.Up * bend;
        Vector2 direction = (Tip - control).Normalized();
        Vector2 baseCenter = Tip - direction * 20;
        Vector2 normal = direction.Orthogonal() * 10;
        var points = new Vector2[33];
        for (int i = 0; i < points.Length; i++)
        {
            float t = i / (float)(points.Length - 1);
            points[i] = (1 - t) * (1 - t) * origin + 2 * (1 - t) * t * control + t * t * baseCenter;
        }
        Color color = Ink;
        DrawPolyline(points, new Color(0, 0, 0, .65f), 8, true);
        DrawPolyline(points, color, 4, true);
        DrawColoredPolygon([Tip, baseCenter + normal, baseCenter - normal], color);
        DrawCircle(origin, 5, color, antialiased: true);
    }
}
