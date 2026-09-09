using Godot;
using System;
using System.Collections.Generic;

namespace MaxGodotUI;

/// <summary>Small host-driven motions. Tweens obey tree pause, never advance domain logic.</summary>
[GlobalClass]
public partial class UiMotion : Node
{
	private readonly Dictionary<ulong, (Control Target, Vector2 Origin, Tween Tween, Action Exiting)> shakes = new();
	public Tween Shake(Control target, float amplitude, double duration)
	{
		StopShake(target);
		Vector2 origin = target.Position;
		ulong id = target.GetInstanceId();
		var tween = target.CreateTween().SetPauseMode(Tween.TweenPauseMode.Stop);
		Action exiting = () => StopShake(target);
		shakes[id] = (target, origin, tween, exiting); target.TreeExiting += exiting;
		tween.TweenMethod(Callable.From<double>(progress =>
		{
			if (IsInstanceValid(target)) target.Position = origin + new Vector2(
				(float)Math.Sin(progress * Math.PI * 8), (float)Math.Sin(progress * Math.PI * 6)) *
				Math.Max(0, amplitude) * (float)(1 - progress);
		}), 0.0, 1.0, Math.Max(.001, duration));
		tween.Finished += () => StopShake(target);
		return tween;
	}
	public void StopShake(Control target)
	{
		if (!IsInstanceValid(target) || !shakes.Remove(target.GetInstanceId(), out var shake)) return;
		shake.Tween.Kill(); target.Position = shake.Origin; target.TreeExiting -= shake.Exiting;
	}
	public override void _ExitTree()
	{
		foreach (var shake in shakes.Values)
		{ shake.Tween.Kill(); if (IsInstanceValid(shake.Target)) { shake.Target.Position = shake.Origin; shake.Target.TreeExiting -= shake.Exiting; } }
		shakes.Clear();
	}
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
