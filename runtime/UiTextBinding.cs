using Godot;
using System;

namespace MaxGodotUI;

/// <summary>Refresh existing controls without disturbing focus, selection or animation.</summary>
[GlobalClass]
public partial class UiTextBinding : Node
{
    private Callable refresh;
    public bool Enabled { get; set; } = true;
    public static void Bind(Node owner, Action update) => Attach(owner, Callable.From(update));
    public static void Attach(Node owner, Callable update)
        => new UiTextBinding().BindTo(owner, update);
    public void BindTo(Node owner, Callable update)
    {
        if (!UiCallback.Valid(update)) throw new ArgumentException("Use a managed delegate or an object-method Callable for refresh.", nameof(update));
        if (GetParent() != null && GetParent() != owner) throw new InvalidOperationException("The binding already belongs to another owner.");
        refresh = update;
        if (GetParent() == null) owner.AddChild(this);
        UiCallback.Invoke(update);
    }
    public static void RefreshTree(Node root)
    {
        if (root is UiTextBinding binding && binding.Enabled && UiCallback.Valid(binding.refresh)) UiCallback.Invoke(binding.refresh);
        foreach (var child in root.GetChildren()) RefreshTree(child);
    }
    public void RefreshBindings(Node root) => RefreshTree(root);
}
