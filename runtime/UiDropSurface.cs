using Godot;

namespace MaxGodotUI;

/// <summary>Variant payloads and callbacks keep hit reception independent of domain validation.</summary>
[GlobalClass]
public partial class UiDropSurface : Control
{
    [Signal] public delegate void PositionClickedEventHandler(Vector2 position);
    [Signal] public delegate void DataDroppedEventHandler(Vector2 position, Variant data);
    public Callable ValidateDrop { get; set; }
    public override void _GuiInput(InputEvent input)
    {
        if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } button)
        { EmitSignal(SignalName.PositionClicked, button.Position); AcceptEvent(); }
    }
    public override bool _CanDropData(Vector2 position, Variant data)
        => UiCallback.Valid(ValidateDrop) && UiCallback.Invoke(ValidateDrop, position, data).AsBool();
    public override void _DropData(Vector2 position, Variant data) => EmitSignal(SignalName.DataDropped, position, data);
}
