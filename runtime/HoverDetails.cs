using Godot;

namespace MaxGodotUI;

/// <summary>Host-supplied content positioned within a shared canvas. Owns no domain data.</summary>
[GlobalClass]
public partial class HoverDetails : PanelContainer
{
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex = 100;
        Hide();
    }

    public void ShowAt(Control content, Vector2 anchor, Vector2 bounds)
    {
        foreach (Node child in GetChildren()) { RemoveChild(child); child.QueueFree(); }
        content.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(content);
        // Container sorting is deferred. Use the supplied child's minimum now so edge placement
        // is correct on the first visible frame as well as after a later layout pass.
        CustomMinimumSize = Vector2.Zero;
        Vector2 extent = content.GetCombinedMinimumSize() + GetThemeStylebox("panel").GetMinimumSize();
        CustomMinimumSize = extent;
        Size = extent;
        Position = new Vector2(
            Mathf.Clamp(anchor.X, 8, Mathf.Max(8, bounds.X - extent.X - 8)),
            Mathf.Clamp(anchor.Y, 8, Mathf.Max(8, bounds.Y - extent.Y - 8)));
        Show();
    }

    public void Close() => Hide();
}
