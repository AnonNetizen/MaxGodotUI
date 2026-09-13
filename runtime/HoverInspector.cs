using Godot;
using System;
using System.Linq;

namespace MaxGodotUI;

/// <summary>Interactive, content-sized entry panels anchored in a host-defined canvas and safe region.</summary>
[GlobalClass]
public partial class HoverInspector : Control
{
    private readonly HoverDetails panel = new() { Name = "Popup" };
    private Control? source;
    public Vector2 Bounds { get; set; } = new(640, 360);
    public float BottomLimit { get; set; } = float.PositiveInfinity;
    public StyleBox EntryStyle { get; set; } = new StyleBoxFlat();
    public StyleBox ContentStyle { get; set; } = new StyleBoxFlat();
    public string ListName { get; set; } = "Entries";
    public HoverDetails Popup => panel;
    private double outsideTime;
    private Vector2 pointer;
    private HBoxContainer? content;
    private VBoxContainer? entryColumn;
    public int ExplanationWidth { get; set; } = 300;
    public int MaxExplanationHeight { get; set; } = 500;
    public bool IsOpen => panel.Visible;
    public bool ContainsPoint(Vector2 point) => IsOpen && (content == null ? panel.GetGlobalRect().HasPoint(point)
        : content.GetChildren().OfType<Control>().Any(child => child.GetGlobalRect().HasPoint(point)));
    public bool PointerInPopup => ContainsPoint(pointer);
    public bool Retains(Control control) => source == control && IsOpen && (PointerInPopup || outsideTime < .20);
    public ScrollContainer? ExplanationScroll { get; private set; }

    public HoverInspector()
    {
        MouseFilter = MouseFilterEnum.Ignore; ZIndex = 100; Name = "HoverInspector";
        ProcessMode = ProcessModeEnum.Always;
        AddChild(panel);
    }
    public void ShowEntries(Control anchor, Godot.Collections.Array<Control> entries, Control? detail = null)
    {
        if (entries.Count == 0 && detail == null) { Close(); return; }
        source = anchor; outsideTime = 0;
        var row = new HBoxContainer { Name = "Content", Theme = Theme };
        content = row; entryColumn = null;
        panel.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
        row.AddThemeConstantOverride("separation", 12);
        if (detail != null) row.AddChild(detail);
        ExplanationScroll = null;
        if (entries.Count > 0)
        {
            var scroll = new ScrollContainer { Name = "Explanations", CustomMinimumSize = new(ExplanationWidth, 0),
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, MouseFilter = MouseFilterEnum.Stop };
            row.AddChild(scroll); ExplanationScroll = scroll;
            var list = new VBoxContainer { Name = ListName, SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Pass };
            list.AddThemeConstantOverride("separation", 8); scroll.AddChild(list); entryColumn = list;
            foreach (var entry in entries)
            {
                var box = new PanelContainer { MouseFilter = MouseFilterEnum.Pass };
                if (entry.Name.ToString().Length > 0) box.Name = entry.Name;
                box.AddThemeStyleboxOverride("panel", EntryStyle); list.AddChild(box); box.AddChild(entry);
            }
            scroll.CustomMinimumSize = new(ExplanationWidth, Math.Min(MaxExplanationHeight, list.GetCombinedMinimumSize().Y));
        }
        panel.ShowAt(row, Vector2.Zero, Bounds);
        FitContents();
        Reposition();
        GetParent()?.MoveChild(this, -1);
    }
    // Plain content can reuse this layer without source retention.
    public void ShowAt(Control content, Vector2 position, Vector2 available)
    {
        source = null; this.content = null; entryColumn = null; ExplanationScroll = null; Bounds = available;
        panel.AddThemeStyleboxOverride("panel", ContentStyle);
        panel.ShowAt(content, position, available); GetParent()?.MoveChild(this, -1);
    }
    public void LeaveSource() { outsideTime = 0; }
    public void Close() { source = null; panel.Close(); ExplanationScroll = null; entryColumn = null; content = null; }
    public override void _Input(InputEvent input)
    {
        if (input is InputEventMouse mouse)
            pointer = GetGlobalTransform() * (GetGlobalTransformWithCanvas().AffineInverse() * mouse.Position);
    }
    public override void _Process(double delta)
    {
        if (!IsOpen || source == null) return;
        if (!GodotObject.IsInstanceValid(source) || !source.IsVisibleInTree() || GetViewport().GuiIsDragging()) { Close(); return; }
        FitContents();
        Reposition();
        var point = pointer;
        bool inside = source.GetGlobalRect().Grow(12).HasPoint(point) || ContainsPoint(point);
        outsideTime = inside ? 0 : outsideTime + delta;
        if (outsideTime > .20) Close();
    }
    private void FitContents()
    {
        if (content == null) return;
        if (ExplanationScroll != null && entryColumn != null)
            ExplanationScroll.CustomMinimumSize = new(ExplanationWidth, Math.Min(MaxExplanationHeight, entryColumn.GetCombinedMinimumSize().Y));
        // Wrapping and locale changes can change the minimum after the first container sort.
        // Reset the plugin's initial extent so a short list can shrink as well as grow.
        Vector2 extent = content.GetCombinedMinimumSize();
        if (!panel.Size.IsEqualApprox(extent))
        { panel.CustomMinimumSize = Vector2.Zero; panel.Size = extent; }
    }
    private void Reposition()
    {
        if (source == null) return;
        Vector2 point = GetGlobalTransform().AffineInverse() * source.GlobalPosition;
        float x = point.X + source.Size.X + 12;
        if (x + panel.Size.X > Bounds.X - 8) x = point.X - panel.Size.X - 12;
        float bottom = Math.Min(Bounds.Y - 8, BottomLimit);
        panel.Position = new(Mathf.Clamp(x, 8, Mathf.Max(8, Bounds.X - panel.Size.X - 8)),
            Mathf.Clamp(point.Y, 8, Mathf.Max(8, bottom - panel.Size.Y)));
    }
}
