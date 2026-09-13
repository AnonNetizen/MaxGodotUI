using Godot;
using System.Collections.Generic;

namespace MaxGodotUI;

/// <summary>Shared shader programs, independent material parameters for each surface.</summary>
[GlobalClass]
public partial class UiShaders : RefCounted
{
    private static readonly Dictionary<string, Shader> shaders = new();
    public static ShaderMaterial Material(string name)
    {
        if (!shaders.TryGetValue(name, out var shader))
            shaders[name] = shader = GD.Load<Shader>($"res://addons/max_godot_ui/shaders/{name}.gdshader");
        return new ShaderMaterial { Shader = shader };
    }
    public ShaderMaterial CreateMaterial(string name) => Material(name);
}
