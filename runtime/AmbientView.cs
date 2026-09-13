using Godot;

namespace MaxGodotUI;

/// <summary>Low-contrast motion over a supplied texture; time is advanced by the host.</summary>
[GlobalClass]
public partial class AmbientView : TextureRect
{
    private ShaderMaterial? shader;
    public override void _Ready() => Material = shader = UiShaders.Material("ambient");
    public void UpdateSurface(double time, bool enabled, bool holdPointer = false)
    {
        if (shader == null) return;
        shader.SetShaderParameter("clock", time);
        shader.SetShaderParameter("strength", enabled ? 1f : 0f);
        if (enabled && holdPointer) return;
        Vector2 pointer = (GetLocalMousePosition() / Size.Max(Vector2.One) - new Vector2(.5f, .5f)).Clamp(-Vector2.One, Vector2.One);
        shader.SetShaderParameter("pointer", enabled ? pointer : Vector2.Zero);
    }
}
