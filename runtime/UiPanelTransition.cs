using Godot;
using System;

namespace MaxGodotUI;

/// <summary>Interruptible panel presentation on a host-owned timeline. No modal or input policy.</summary>
[GlobalClass]
public partial class UiPanelTransition : Node
{
    [Signal] public delegate void CompletedEventHandler(bool shown);
    private UiTimeline? timeline;
    private Control? visual;
    private Vector2 shownPosition, hiddenPosition;
    private Color tint = Colors.White;
    private int channel;
    private bool sliding = true;
    public bool TargetShown { get; private set; }
    public bool IsTransitioning { get; private set; }
    public float Amount { get; private set; }

    public void Configure(UiTimeline clock, Control content, Vector2 shown, Vector2 hidden, int timeChannel)
    {
        Cancel();
        if (IsInstanceValid(visual)) visual!.TreeExiting -= TargetExiting;
        timeline = clock; visual = content; shownPosition = shown; hiddenPosition = hidden; channel = timeChannel;
        tint = content.Modulate; TargetShown = false; Amount = 0;
        content.TreeExiting += TargetExiting;
        Sample(0);
    }

    public void SetPresentation(bool slide)
    {
        if (sliding == slide) return;
        sliding = slide; Finish(); Sample(Amount);
    }

    public void Enter(double seconds) => SetShown(true, seconds);
    public void Exit(double seconds) => SetShown(false, seconds);
    private void SetShown(bool shown, double seconds)
    {
        if (!Alive || TargetShown == shown && (IsTransitioning || Amount == (shown ? 1 : 0))) return;
        Cancel(); TargetShown = shown;
        float start = Amount, end = shown ? 1 : 0;
        IsTransitioning = true;
        if (seconds <= 0 || Math.Abs(end - start) < .0001f) { Finish(); return; }
        timeline!.Animate(visual!, seconds * Math.Abs(end - start), Callable.From<float>(p =>
        {
            float eased = shown ? UiTimeline.Ease(p) : p * p * p;
            Sample(Mathf.Lerp(start, end, eased));
        }), channel, Callable.From(Complete));
    }

    public void Finish()
    {
        if (!IsTransitioning) return;
        Cancel(); Sample(TargetShown ? 1 : 0);
        if (Alive) EmitSignal(SignalName.Completed, TargetShown);
    }
    public void Cancel()
    {
        if (IsInstanceValid(timeline) && IsInstanceValid(visual)) timeline!.CancelTarget(visual!);
        IsTransitioning = false;
    }
    private bool Alive => IsInstanceValid(visual) && visual!.IsInsideTree();
    private void Sample(float amount)
    {
        if (!Alive) return;
        Amount = amount;
        visual!.Position = sliding ? hiddenPosition.Lerp(shownPosition, amount) : shownPosition;
        visual.Modulate = new(tint.R, tint.G, tint.B, tint.A * (sliding ? 1 : amount));
    }
    private void Complete()
    {
        IsTransitioning = false;
        if (Alive) EmitSignal(SignalName.Completed, TargetShown);
    }
    private void TargetExiting() { Cancel(); visual = null; }
    public override void _ExitTree()
    {
        Cancel();
        if (IsInstanceValid(visual)) visual!.TreeExiting -= TargetExiting;
        visual = null; timeline = null;
    }
}
