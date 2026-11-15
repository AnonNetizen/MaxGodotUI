using Godot;
using System;

namespace MaxGodotUI;

public enum UiArrowPath { Curved, Straight }

/// <summary>Input-transparent directional decoration with a caller-owned animation clock.</summary>
[GlobalClass]
public partial class UiArrow : Control
{
    private Control? source, target;
    private Vector2 point;
    private float edgeGap;
    private double time;
    [Export] public UiArrowPath Path { get; set; } = UiArrowPath.Curved;
    public int Detail { get; private set; } = 2;
    public float FlowOffset => Detail == 2 ? (float)(time * 56 % 28) : 0;
    public bool HasSegment => TrySegment(out _, out _);
    // Tweens advance after node processing. Resolve the anchor at draw time so it never trails
    // a moving source by one frame; the pointer and legality can still update through normal input.
    public Vector2 Origin { get { TrySegment(out var origin, out _); return origin; } }
    public Vector2 Tip { get { TrySegment(out _, out var tip); return tip; } }
    public Vector2 AnchorPoint { get; private set; }
    public Color Ink { get; private set; } = Colors.White;

    public UiArrow()
    {
        ZIndex = 80;
        MouseFilter = MouseFilterEnum.Ignore; Visible = false;
    }

    public void PointFrom(Control visual, Vector2 anchor, Vector2 tip, Color color)
    {
        source = visual; target = null; AnchorPoint = anchor; point = tip; Ink = color;
        Visible = new Rect2(Vector2.Zero, Size).HasPoint(tip);
        QueueRedraw();
    }

    /// <summary>Joins transformed control borders, leaving the segment outside both surfaces.</summary>
    public void PointBetween(Control from, Control to, Color color, float gap = 6)
    {
        source = from; target = to; Ink = color; edgeGap = Math.Max(0, gap);
        Visible = HasSegment; QueueRedraw();
    }

    public void SetDetail(int detail) { Detail = Math.Clamp(detail, 0, 2); QueueRedraw(); }
    public void UpdateSurface(double seconds) { time = Math.Max(0, seconds); QueueRedraw(); }

    private bool TrySegment(out Vector2 origin, out Vector2 tip)
    {
        origin = Vector2.Zero; tip = point;
        if (!IsInstanceValid(source) || !source!.IsInsideTree() || !source.IsVisibleInTree()) return false;
        var frame = GetGlobalTransformWithCanvas().AffineInverse();
        var from = frame * source.GetGlobalTransformWithCanvas();
        if (target == null)
        {
            origin = from * AnchorPoint;
            return new Rect2(Vector2.Zero, Size).HasPoint(tip) && origin.DistanceTo(tip) >= 8;
        }
        if (!IsInstanceValid(target) || !target.IsInsideTree() || !target.IsVisibleInTree() || source.Size.X <= 0 || source.Size.Y <= 0 || target.Size.X <= 0 || target.Size.Y <= 0) return false;
        var to = frame * target.GetGlobalTransformWithCanvas();
        var fromCenter = from * (source.Size / 2);
        var toCenter = to * (target.Size / 2);
        var direction = (toCenter - fromCenter).Normalized();
        origin = Border(from, source.Size, toCenter) + direction * edgeGap;
        tip = Border(to, target.Size, fromCenter) - direction * edgeGap;
        return (tip - origin).Dot(direction) >= 8 && new Rect2(Vector2.Zero, Size).HasPoint(tip);
    }

    private static Vector2 Border(Transform2D transform, Vector2 size, Vector2 towards)
    {
        var half = size / 2;
        var delta = transform.AffineInverse() * towards - half;
        float ratio = Math.Min(Math.Abs(delta.X) > .001f ? half.X / Math.Abs(delta.X) : float.PositiveInfinity,
            Math.Abs(delta.Y) > .001f ? half.Y / Math.Abs(delta.Y) : float.PositiveInfinity);
        return transform * (half + delta * (float.IsFinite(ratio) ? ratio : 0));
    }

    public override void _Draw()
    {
        if (!TrySegment(out var origin, out var tip)) return;
        if (Path == UiArrowPath.Straight) DrawStraight(origin, tip);
        else DrawCurve(origin, tip);
    }

    private void DrawStraight(Vector2 origin, Vector2 tip)
    {
        float length = origin.DistanceTo(tip);
        var direction = (tip - origin) / length;
        var normal = direction.Orthogonal();
        float head = Math.Min(24, length * .45f);
        var end = tip - direction * head;
        var outline = new Color(.025f, .035f, .055f, Ink.A * .92f);
        if (Detail == 2) DrawLine(origin, end, new Color(Ink, Ink.A * .14f), 15, true);
        DrawLine(origin, end, outline, 10, true);
        DrawLine(origin, end, Ink, 6, true);
        if (Detail > 0)
            for (float offset = FlowOffset; offset < length - head; offset += 28)
            {
                float fade = Math.Clamp(Math.Min(offset, length - head - offset) / 9, 0, 1);
                var marker = origin + direction * offset;
                DrawPolyline([marker - direction * 6 + normal * 3.5f, marker, marker - direction * 6 - normal * 3.5f],
                    new Color(Ink.Lightened(.75f), Ink.A * fade * .9f), 2.5f, true);
            }
        float width = head * .58f;
        Vector2[] triangle = [tip, end + normal * width, end - normal * width];
        DrawColoredPolygon(triangle, Ink.Lightened(.08f));
        DrawPolyline([triangle[0], triangle[1], triangle[2], triangle[0]], outline, 2.5f, true);
    }

    private void DrawCurve(Vector2 origin, Vector2 tip)
    {
        if (origin.DistanceTo(tip) < 24) return;
        // Leave the source vertically, then bend toward the pointer without snapping its tip.
        float bend = Math.Min(180, origin.DistanceTo(tip) * .35f);
        Vector2 control = origin + Vector2.Up * bend;
        Vector2 direction = (tip - control).Normalized();
        Vector2 baseCenter = tip - direction * 20;
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
        DrawColoredPolygon([tip, baseCenter + normal, baseCenter - normal], color);
        DrawCircle(origin, 5, color, antialiased: true);
    }
}
