using Godot;

namespace MaxGodotUI;

/// <summary>Small control factories. Appearance and content always come from the caller.</summary>
[GlobalClass]
public partial class UiControls : RefCounted
{
    private static ShaderMaterial? rectangleEdges;
    public TextureRect Texture(Control parent, Texture2D texture, Rect2 rect, float opacity = 1)
    {
        var view = new TextureRect { ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            Texture = texture, Position = rect.Position, Size = rect.Size,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered, ClipContents = true,
            MouseFilter = Control.MouseFilterEnum.Ignore, Modulate = new Color(1, 1, 1, opacity) };
        parent.AddChild(view); view.Size = rect.Size; return view;
    }
    public MarginContainer PadScroll(ScrollContainer scroll, Control content, int padding)
    {
        var margin = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Pass };
        foreach (string edge in new[] { "left", "top", "right", "bottom" })
            margin.AddThemeConstantOverride("margin_" + edge, padding);
        scroll.AddChild(margin); margin.AddChild(content); return margin;
    }
    public ColorRect Rule(Control parent, Rect2 rect, Color color)
    {
        var rule = new ColorRect { Position = rect.Position, Size = rect.Size, Color = color,
            Material = rectangleEdges ??= UiShaders.Material("rectangle_edges"), MouseFilter = Control.MouseFilterEnum.Ignore };
        parent.AddChild(rule); return rule;
    }
    public Label Label(Control parent, string text, Rect2 rect, int size, Color color, HorizontalAlignment alignment = HorizontalAlignment.Left)
    {
        var label = new Label { MouseFilter = Control.MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.WordSmart, HorizontalAlignment = alignment, VerticalAlignment = VerticalAlignment.Center };
        label.AddThemeFontSizeOverride("font_size", size); label.AddThemeColorOverride("font_color", color);
        parent.AddChild(label);
        // Establish wrapping width before text shapes the minimum size.
        label.Position = rect.Position; label.Size = rect.Size; label.Text = text;
        return label;
    }
    public Button Button(Control parent, string text, Rect2 rect)
    {
        var button = new Button { Text = text, Position = rect.Position, Size = rect.Size,
            FocusMode = Control.FocusModeEnum.None, MouseDefaultCursorShape = Control.CursorShape.PointingHand };
        parent.AddChild(button); return button;
    }
    public Panel Panel(Control parent, Rect2 rect, StyleBox style)
    {
        var panel = new Panel { Position = rect.Position, Size = rect.Size, MouseFilter = Control.MouseFilterEnum.Ignore };
        panel.AddThemeStyleboxOverride("panel", style); parent.AddChild(panel); return panel;
    }
}
