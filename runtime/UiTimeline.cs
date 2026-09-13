using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MaxGodotUI;

/// <summary>Bounded, explicitly advanced visual transitions. Channels have no domain meaning.</summary>
[GlobalClass]
public partial class UiTimeline : Node
{
    private sealed class Transition(Control owner, double duration, Callable sample, int channel, Callable complete, int particles)
    {
        public readonly Control Owner = owner;
        public readonly double Duration = Math.Max(.001, duration);
        public readonly Callable Sample = sample, Complete = complete;
        public readonly int Channel = channel, Particles = particles;
        public double Elapsed;
    }
    private readonly List<Transition> active = new();
    public int MaxEffects { get; set; } = 24;
    public int MaxParticles { get; set; } = 256;
    public int ActiveCount => active.Count;
    public int ParticleCount => active.Sum(t => t.Particles);
    public bool Immediate { get; set; }
    public double DurationLimit { get; set; } = double.PositiveInfinity;

    public void Advance(int channel, double delta)
    {
        foreach (var t in active.ToArray())
        {
            if (!Alive(t.Owner)) { active.Remove(t); continue; }
            if (t.Channel != channel || !active.Contains(t) || delta <= 0) continue;
            t.Elapsed += delta;
            float p = (float)Math.Clamp(t.Elapsed / t.Duration, 0, 1);
            // Remove before invoking user code: callbacks may cancel or replace transitions.
            if (p >= 1) active.Remove(t);
            UiCallback.Invoke(t.Sample, p);
            if (p >= 1 && UiCallback.Valid(t.Complete)) UiCallback.Invoke(t.Complete);
        }
    }
    private static bool Alive(Control owner) => IsInstanceValid(owner) && owner.IsInsideTree();
    private void Finish(Transition t, bool finish)
    {
        if (!active.Remove(t) || !Alive(t.Owner) || !finish) return;
        UiCallback.Invoke(t.Sample, 1f);
        if (UiCallback.Valid(t.Complete)) UiCallback.Invoke(t.Complete);
    }
    public void Animate(Control owner, double seconds, Callable sample, int channel = 0, Callable complete = default, int particles = 0)
    {
        if (!UiCallback.Valid(sample)) throw new ArgumentException("Use a managed delegate or an object-method Callable for sampling.", nameof(sample));
        foreach (var t in active.Where(t => t.Owner == owner).ToArray()) Finish(t, true);
        if (!Alive(owner)) return;
        particles = Math.Max(0, particles);
        if (Immediate || MaxEffects <= 0 || particles > Math.Max(0, MaxParticles))
        { UiCallback.Invoke(sample, 1f); if (UiCallback.Valid(complete)) UiCallback.Invoke(complete); return; }
        while (active.Count >= MaxEffects || ParticleCount + particles > Math.Max(0, MaxParticles)) Finish(active[0], true);
        active.Add(new(owner, Math.Min(seconds, DurationLimit), sample, channel, complete, particles));
        UiCallback.Invoke(sample, 0f);
    }
    public void Clear(Control root, bool finish = true)
    {
        foreach (var t in active.Where(t => !Alive(t.Owner) || root == t.Owner || root.IsAncestorOf(t.Owner)).ToArray()) Finish(t, finish);
    }
    public void FinishAll()
    { foreach (var t in active.ToArray()) Finish(t, true); }
    public override void _ExitTree() => active.Clear();
    public void Enter(Control owner, double delay = 0, int channel = 0)
        => Animate(owner, .18 + delay, Callable.From<float>(p => owner.Modulate = new(1, 1, 1,
            Mathf.Clamp((float)((p * (.18 + delay) - delay) / .18), 0, 1))), channel);
    public void Pulse(Control owner, Color color, int channel = 0)
        => Animate(owner, .18, Callable.From<float>(p => owner.SelfModulate = color.Lerp(Colors.White, Ease(p))), channel);
    public static float Ease(float p) => 1 - MathF.Pow(1 - p, 3);
}
