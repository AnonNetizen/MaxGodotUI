using Godot;
using System;

namespace MaxGodotUI;

/// <summary>Pointer response on a visual child. The layout and input owner never move.</summary>
[GlobalClass]
public partial class SurfaceMotion : RefCounted
{
    [Signal] public delegate void TransformedEventHandler();
    public Control? Visual { get; set; }
    public StyleBoxFlat? Shadow { get; set; }
    public bool Inertial { get; set; }
    public bool Stable { get; set; }
    public bool Resting { get; set; }
    public bool Selected { get; set; }
    public bool ShakeEnabled { get; set; }
    public float ReadingTop { get; set; } = float.PositiveInfinity;
    public float Phase { get; set; }
    public Vector2 Offset { get; set; }
    public float Impact => Math.Max(0, 1 - impulseAge / .20f);
    public bool Triggered => impulseAge < .20f;
    private bool hovered;
    private float hoverAge, impulseAge = 1, shakeAge = 1;
    private float angle, velocity, time, pointerStillAge;
    private Vector2 previousPointer;

    public void SetHover(bool value, Vector2 pointer, float direction, bool enabled)
    {
        if (hovered == value) return;
        hovered = value; hoverAge = 0; previousPointer = pointer; pointerStillAge = 0;
        if (Inertial && enabled) velocity = value ? direction * 48 : velocity * .5f;
    }
    public void Trigger() => impulseAge = 0;
    public void Shake() => shakeAge = 0;
    public bool Step(double delta, Vector2 pointer, Vector2 normalized, bool enabled, bool frozen, float triggerSpeed = 1, bool force = false)
    {
        if (Visual == null || !IsInstanceValid(Visual)) return false;
        if (!frozen) { hoverAge += (float)delta; impulseAge += (float)delta * triggerSpeed; shakeAge += (float)delta; }
        bool active = force || hovered || Selected || hoverAge <= .22f || impulseAge <= .22f || shakeAge <= .20f || enabled && Inertial ||
            Offset != Vector2.Zero || Math.Abs(Visual.Rotation) > .0001f || Visual.Position != Vector2.Zero || Visual.Scale != Vector2.One;
        if (!active || frozen && enabled) return false;
        float tilt = enabled && (hovered || Selected) && !Stable ? Mathf.Clamp((normalized.X - .5f) * 6, -3, 3) : 0;
        if (Inertial) tilt = Sway((float)delta, pointer, normalized, enabled);
        Visual.Rotation = Inertial ? Mathf.DegToRad(tilt)
            : enabled ? Mathf.Lerp(Visual.Rotation, Mathf.DegToRad(tilt), Math.Min(1, (float)delta * 22)) : 0;
        float spring = enabled && hovered && (Inertial || !Stable) ? -4 * MathF.Sin(Math.Min(1, hoverAge / .20f) * Mathf.Pi) : 0;
        float kick = enabled && impulseAge < .20f ? MathF.Sin(impulseAge / .20f * Mathf.Pi) : 0;
        float recoil = enabled && ShakeEnabled && shakeAge < .18f ? MathF.Sin(shakeAge * 135) * (1 - shakeAge / .18f) * 4 : 0;
        Visual.Position = (enabled ? Offset : Vector2.Zero) + new Vector2(recoil, spring - 5 * kick);
        Visual.Scale = Vector2.One * (1 + .035f * kick);
        if (Shadow != null)
        {
            Shadow.ShadowSize = hovered && enabled ? 14 : 7;
            Shadow.ShadowOffset = new(tilt * -.6f, hovered && enabled ? 8 : 4);
        }
        EmitSignal(SignalName.Transformed);
        return true;
    }
    private float Sway(float delta, Vector2 pointer, Vector2 local, bool enabled)
    {
        if (!enabled) { angle = velocity = 0; previousPointer = pointer; return 0; }
        float step = Math.Min(delta, .05f);
        time += step;
        Vector2 travel = pointer - previousPointer; previousPointer = pointer;
        pointerStillAge = travel.LengthSquared() > 1 ? 0 : pointerStillAge + step;
        if (hovered) velocity = Mathf.Clamp(velocity + travel.X / Math.Max(1, Visual!.Size.X) * 65, -75, 75);
        bool reading = pointerStillAge > .35f && local.Y >= ReadingTop;
        float target = hovered && !reading ? Mathf.Clamp((local.X - .5f) * 4.4f, -2.2f, 2.2f) : 0;
        if (Resting) target += .38f * MathF.Sin(time * 2.3f + Phase);
        for (float remaining = step; remaining > 0;)
        {
            float dt = Math.Min(remaining, 1f / 120);
            velocity += ((target - angle) * 190 - velocity * 15) * dt;
            angle += velocity * dt;
            if (Math.Abs(angle) > 3) { angle = Mathf.Clamp(angle, -3, 3); velocity = 0; }
            remaining -= dt;
        }
        return angle;
    }
}
