using Godot;
using System;

namespace MaxGodotUI;

/// <summary>Input-transparent directional trajectory, energy beam or slash effect driven by caller progress.</summary>
[GlobalClass]
public partial class UiProjectile : Control
{
    public Vector2 Origin { get; set; }
    public Vector2 Tip { get; set; }
    public Color Ink { get; set; } = Colors.White;
    public Color CoreInk { get; set; } = new(1f, .98f, .92f, 1f);
    public Color OutlineInk { get; set; } = new(.04f, .06f, .11f, .92f);
    public string Style { get; set; } = "beam";
    public float Width { get; set; } = 12f;
    public long Seed { get; set; }
    public int Particles { get; set; }
    public float Progress { get; private set; }
    public bool Detailed { get; private set; } = true;

    public UiProjectile()
    {
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public void Configure(Vector2 origin, Vector2 tip, Color ink, string style = "beam", float width = 12f, long seed = 0, int particles = 0)
    {
        Origin = origin;
        Tip = tip;
        Ink = ink;
        Style = style;
        Width = Math.Max(2f, width);
        Seed = seed;
        Particles = Math.Max(0, particles);
        QueueRedraw();
    }

    public void Sample(float progress, bool detailed = true)
    {
        Progress = Math.Clamp(progress, 0f, 1f);
        Detailed = detailed;
        float alpha = Style == "slash"
            ? MathF.Sin(Math.Min(1f, Progress * 1.15f) * MathF.PI * .5f) * (1f - Progress * Progress)
            : 1f - MathF.Pow(Math.Max(0f, (Progress - .45f) / .55f), 2f);
        Modulate = new(1f, 1f, 1f, Math.Clamp(alpha, 0f, 1f));
        QueueRedraw();
    }

    public override void _Draw()
    {
        Vector2 delta = Tip - Origin;
        float length = delta.Length();
        if (length < 4f) return;
        Vector2 dir = delta / length;
        Vector2 normal = dir.Orthogonal();

        if (Style == "slash")
        {
            DrawSlash(Tip, dir, normal);
            return;
        }
        if (Style == "arc")
        {
            DrawArcStream(dir, normal, length);
            return;
        }
        if (Style == "bolt")
        {
            DrawBolt(dir, normal, length);
            return;
        }
        DrawBeam(dir, normal, length);
    }

    private void DrawBeam(Vector2 dir, Vector2 normal, float length)
    {
        float reach = UiTimeline.Ease(Math.Clamp(Progress / .34f, 0f, 1f));
        float tail = UiTimeline.Ease(Math.Clamp((Progress - .28f) / .72f, 0f, 1f));
        Vector2 start = Origin + dir * (length * tail * .68f);
        Vector2 end = Origin + dir * (length * reach);
        if (start.DistanceTo(end) < 3f) return;

        float fade = 1f - Progress * Progress * .75f;
        float w = Width * (1.15f - .55f * Progress);
        if (Detailed) DrawLine(start, end, new Color(Ink, .22f * fade), w * 2.4f, true);
        DrawLine(start, end, new Color(OutlineInk, OutlineInk.A * fade), w + 5f, true);
        DrawLine(start, end, new Color(Ink, .95f * fade), w, true);
        DrawLine(start, end, new Color(CoreInk, .95f * fade), Math.Max(2f, w * .44f), true);

        if (Detailed)
        {
            float headR = w * (1.2f + .5f * (1f - Progress));
            DrawCircle(end, headR * 1.35f, new Color(Ink, .26f * fade), true, -1, true);
            DrawCircle(end, headR * .65f, new Color(CoreInk, .92f * fade), true, -1, true);
            DrawSparks(start, end, dir, normal, fade);
        }
    }

    private void DrawBolt(Vector2 dir, Vector2 normal, float length)
    {
        float headT = UiTimeline.Ease(Math.Clamp(Progress / .72f, 0f, 1f));
        float tailT = Math.Max(0f, headT - (Detailed ? .38f : .22f));
        Vector2 head = Origin + dir * (length * headT);
        Vector2 tail = Origin + dir * (length * tailT);
        float w = Width * (1f - .35f * Progress);
        float fade = 1f - Progress * Progress * .6f;

        Vector2[] spear = [
            head + dir * (w * 1.4f),
            head - dir * (w * .5f) + normal * (w * .75f),
            tail,
            head - dir * (w * .5f) - normal * (w * .75f)
        ];
        if (Detailed) DrawLine(tail, head, new Color(Ink, .22f * fade), w * 2.2f, true);
        DrawColoredPolygon(spear, new Color(Ink, .96f * fade));
        DrawPolyline([spear[0], spear[1], spear[2], spear[3], spear[0]], new Color(OutlineInk, .9f * fade), 2.5f, true);

        Vector2[] core = [
            head + dir * (w * .85f),
            head - dir * (w * .3f) + normal * (w * .32f),
            head.Lerp(tail, .65f),
            head - dir * (w * .3f) - normal * (w * .32f)
        ];
        DrawColoredPolygon(core, new Color(CoreInk, .95f * fade));
        if (Detailed) DrawSparks(tail, head, dir, normal, fade);
    }

    private void DrawArcStream(Vector2 dir, Vector2 normal, float length)
    {
        Vector2 side = normal.Y < 0 ? normal : -normal;
        float lift = Math.Min(150f, length * .32f);
        Vector2 control = Origin.Lerp(Tip, .5f) + side * lift;
        float headT = UiTimeline.Ease(Math.Clamp(Progress / .76f, 0f, 1f));
        float tailT = Math.Max(0f, headT - .42f);
        const int steps = 16;
        var points = new Vector2[steps + 1];
        for (int i = 0; i <= steps; i++)
        {
            float t = Mathf.Lerp(tailT, headT, i / (float)steps);
            points[i] = Quadratic(Origin, control, Tip, t);
        }
        float fade = 1f - Progress * Progress * .7f;
        float w = Width * .85f;
        if (Detailed) DrawPolyline(points, new Color(Ink, .24f * fade), w * 2.3f, true);
        DrawPolyline(points, new Color(OutlineInk, .85f * fade), w + 4f, true);
        DrawPolyline(points, new Color(Ink, .94f * fade), w, true);
        DrawPolyline(points, new Color(CoreInk, .9f * fade), Math.Max(2f, w * .4f), true);
        if (Detailed) DrawCircle(points[^1], w * .95f, new Color(CoreInk, .92f * fade), true, -1, true);
    }

    private void DrawSlash(Vector2 center, Vector2 dir, Vector2 normal)
    {
        float sweep = UiTimeline.Ease(Math.Clamp(Progress / .55f, 0f, 1f));
        float radius = Width * 4.4f + Progress * 18f;
        float baseAngle = dir.Angle();
        float startAngle = baseAngle - 1.18f;
        float endAngle = Mathf.Lerp(startAngle + .25f, baseAngle + 1.18f, sweep);
        const int segs = 12;
        var outer = new Vector2[segs + 1];
        var inner = new Vector2[segs + 1];
        float thickness = Width * 1.45f * (1f - Progress * .65f);
        for (int i = 0; i <= segs; i++)
        {
            float u = i / (float)segs;
            float angle = Mathf.Lerp(startAngle, endAngle, u);
            float taper = MathF.Sin(u * MathF.PI);
            Vector2 radial = Vector2.Right.Rotated(angle);
            Vector2 arcCenter = center - dir * (radius * .35f);
            outer[i] = arcCenter + radial * (radius + thickness * .45f * taper);
            inner[segs - i] = arcCenter + radial * (radius - thickness * .65f * taper);
        }
        var crescent = new Vector2[(segs + 1) * 2];
        Array.Copy(outer, 0, crescent, 0, segs + 1);
        Array.Copy(inner, 0, crescent, segs + 1, segs + 1);
        float fade = 1f - Progress * Progress;
        if (Detailed) DrawPolyline(outer, new Color(Ink, .28f * fade), thickness * 1.6f, true);
        DrawColoredPolygon(crescent, new Color(Ink, .94f * fade));
        DrawPolyline(outer, new Color(OutlineInk, .88f * fade), 2.5f, true);
        DrawPolyline(outer, new Color(CoreInk, .95f * fade), Math.Max(2f, thickness * .32f), true);
    }

    private void DrawSparks(Vector2 start, Vector2 end, Vector2 dir, Vector2 normal, float fade)
    {
        int count = Math.Min(Particles, 12);
        for (int i = 0; i < count; i++)
        {
            float frac = ((i * .37f + (Seed % 31) * .03f) % 1f);
            float side = ((i % 2 == 0 ? 1f : -1f) * (6f + (i * 7 % 17)) * (1f + Progress * .8f));
            Vector2 pos = start.Lerp(end, frac) + normal * side;
            Vector2 tail = pos - dir * (8f + i % 5 * 3f);
            DrawLine(pos, tail, new Color(i % 3 == 0 ? CoreInk : Ink, .85f * fade), 2f, true);
        }
    }

    private static Vector2 Quadratic(Vector2 a, Vector2 b, Vector2 c, float t)
    {
        float inv = 1f - t;
        return inv * inv * a + 2f * inv * t * b + t * t * c;
    }
}
