using Godot;
using System;

namespace MaxGodotUI;

/// <summary>Content-free parameters for the procedural background shader contract.</summary>
[GlobalClass]
public partial class BackgroundStyle : Resource
{
    [Export] public Shader? Shader { get; set; }
    [Export] public Color BaseColor { get; set; } = new(.025f, .025f, .025f);
    [Export] public Color PrimaryColor { get; set; } = new(.5f, .5f, .5f);
    [Export] public Color SecondaryColor { get; set; } = new(.2f, .2f, .2f);
    [Export] public float Speed { get; set; } = 1;
    [Export] public float Intensity { get; set; } = 1;
    [Export] public Vector2 Center { get; set; } = new(.5f, .5f);
    [Export] public int Seed { get; set; } = 17;

    public ShaderMaterial CreateMaterial()
    {
        if (Shader == null) throw new InvalidOperationException("A background style requires a shader.");
        var material = new ShaderMaterial { Shader = Shader };
        material.SetShaderParameter("base_color", BaseColor);
        material.SetShaderParameter("primary_color", PrimaryColor);
        material.SetShaderParameter("secondary_color", SecondaryColor);
        material.SetShaderParameter("flow_speed", Speed);
        material.SetShaderParameter("intensity", Intensity);
        material.SetShaderParameter("center", Center);
        material.SetShaderParameter("seed", Seed);
        return material;
    }
}
