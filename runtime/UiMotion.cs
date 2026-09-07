using Godot;

namespace MaxGodotUI;

/// <summary>Small host-driven motions. Tweens obey tree pause, never advance domain logic.</summary>
[GlobalClass]
public partial class UiMotion : Node
{
    public Tween Move(Control target, Vector2 destination, double duration)
    {
        Tween tween = target.CreateTween().SetPauseMode(Tween.TweenPauseMode.Stop);
        tween.TweenProperty(target, "position", destination, duration)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        return tween;
    }

    public Tween Flash(CanvasItem target, Color color, double duration)
    {
        Color original = target.Modulate;
        target.Modulate = color;
        Tween tween = target.CreateTween().SetPauseMode(Tween.TweenPauseMode.Stop);
        tween.TweenProperty(target, "modulate", original, duration);
        return tween;
    }
}
