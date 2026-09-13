using Godot;

namespace MaxGodotUI;

/// <summary>Host-supplied colors and metrics; no assets or domain palette.</summary>
[GlobalClass]
public partial class UiStyles : Resource
{
    [Export] public Color Background { get; set; } = Colors.Black;
    [Export] public Color Panel { get; set; } = new Color(.12f, .12f, .12f);
    [Export] public Color Border { get; set; } = Colors.Gray;
    [Export] public Color Text { get; set; } = Colors.White;
    [Export] public Color Muted { get; set; } = Colors.Gray;
    [Export] public Color Accent { get; set; } = Colors.White;
    [Export] public int BodySize { get; set; } = 24;
    [Export] public int ButtonSize { get; set; } = 23;
    [Export] public int PaddingX { get; set; } = 12;
    [Export] public int PaddingY { get; set; } = 8;
    public StyleBoxFlat Box(Color fill, Color border, int radius = 4, int width = 1)
        => new() { BgColor = fill, BorderColor = border, AntiAliasing = true, AntiAliasingSize = 1, CornerDetail = 12,
            BorderWidthLeft = width, BorderWidthTop = width, BorderWidthRight = width, BorderWidthBottom = width,
            CornerRadiusTopLeft = radius, CornerRadiusTopRight = radius,
            CornerRadiusBottomLeft = radius, CornerRadiusBottomRight = radius,
            ContentMarginLeft = PaddingX, ContentMarginRight = PaddingX, ContentMarginTop = PaddingY, ContentMarginBottom = PaddingY };

    public Theme Create(Font font)
    {
        var theme = new Theme { DefaultFont = font, DefaultFontSize = BodySize };
        foreach (string type in new[] { "Button", "OptionButton", "CheckButton", "CheckBox" })
        {
            theme.SetStylebox("normal", type, Box(Panel, Border));
            theme.SetStylebox("hover", type, Box(Panel.Lightened(.12f), Accent));
            theme.SetStylebox("pressed", type, Box(Background, Accent));
            theme.SetStylebox("disabled", type, Box(Background, Border.Darkened(.2f)));
            theme.SetStylebox("focus", type, Box(Colors.Transparent, Accent, 4, 2));
            theme.SetColor("font_color", type, Text);
            theme.SetColor("font_hover_color", type, Text);
            theme.SetColor("font_pressed_color", type, Accent);
            theme.SetColor("font_disabled_color", type, Muted.Darkened(.18f));
        }
        theme.SetStylebox("normal", "LineEdit", Box(Background, Border));
        theme.SetStylebox("read_only", "LineEdit", Box(Panel, Border));
        theme.SetStylebox("focus", "LineEdit", Box(Colors.Transparent, Accent, 4, 2));
        theme.SetColor("font_color", "LineEdit", Text);
        theme.SetColor("caret_color", "LineEdit", Accent);
        theme.SetColor("selection_color", "LineEdit", new Color(Accent, .3f));
        theme.SetColor("font_placeholder_color", "LineEdit", Muted);
        theme.SetStylebox("panel", "PopupMenu", Box(Panel, Border));
        theme.SetStylebox("hover", "PopupMenu", Box(new Color(Accent, .16f), Accent));
        theme.SetColor("font_color", "PopupMenu", Text);
        theme.SetColor("font_hover_color", "PopupMenu", Text);
        theme.SetColor("font_disabled_color", "PopupMenu", Muted);
        theme.SetConstant("v_separation", "PopupMenu", 12);
        foreach (string type in new[] { "HScrollBar", "VScrollBar", "HSlider", "VSlider" })
        {
            var track = Box(Background, Background, 2); track.ContentMarginLeft = track.ContentMarginRight = 4;
            track.ContentMarginTop = track.ContentMarginBottom = 4;
            theme.SetStylebox(type.Contains("Slider") ? "slider" : "scroll", type, track);
            foreach (string state in new[] { "grabber", "grabber_highlight", "grabber_pressed", "grabber_area", "grabber_area_highlight" })
            {
                var grip = Box(state.Contains("highlight") ? Accent : Border.Lightened(.15f), Colors.Transparent, 2, 0);
                grip.ContentMarginLeft = grip.ContentMarginRight = grip.ContentMarginTop = grip.ContentMarginBottom = 5;
                theme.SetStylebox(state, type, grip);
            }
        }
        return theme;
    }

    public void StyleButton(Button button, bool primary = false)
    {
        button.AddThemeFontSizeOverride("font_size", ButtonSize);
        button.AddThemeStyleboxOverride("normal", Box(primary ? Accent : Panel, primary ? Accent : Border));
        button.AddThemeStyleboxOverride("hover", Box(primary ? Accent.Lightened(.12f) : Panel.Lightened(.2f), Accent));
        button.AddThemeStyleboxOverride("pressed", Box(primary ? Accent.Darkened(.15f) : Panel.Darkened(.1f), Accent));
        button.AddThemeStyleboxOverride("disabled", Box(Panel.Darkened(.2f), Border));
        button.AddThemeColorOverride("font_color", primary ? Background : Text);
        button.AddThemeColorOverride("font_hover_color", primary ? Background : Text);
        button.AddThemeColorOverride("font_pressed_color", primary ? Background : Text);
        button.AddThemeColorOverride("font_disabled_color", Muted.Darkened(.12f));
    }
}
