using Godot;
using System;

namespace MaxGodotUI;

/// <summary>Input-transparent single-pass atmospheric lighting, drifting motes and halftone vignette.</summary>
[GlobalClass]
public partial class UiSceneAtmosphere : ColorRect
{
    [Export] public Vector2 LightOrigin { get; set; } = new(.5f, .08f);
    [Export] public Color LightColor { get; set; } = new(.39f, .89f, .83f, 1f);
    [Export] public Color ShadowColor { get; set; } = new(.04f, .06f, .11f, 1f);
    [Export] public float BeamStrength { get; set; } = .22f;
    [Export] public float DustStrength { get; set; } = .28f;
    [Export] public float HalftoneStrength { get; set; } = .07f;
    [Export] public float VignetteStrength { get; set; } = .34f;
    [Export] public float Seed { get; set; } = 17f;

    public int Detail { get; private set; } = 2;
    public double ClockTime { get; private set; }
    public bool MaterialActive => shaderMaterial != null;

    private ShaderMaterial? shaderMaterial;
    private Vector2 pointer;
    private bool dirty = true;

    public UiSceneAtmosphere()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Color = Colors.White;
    }

    public void Configure(Vector2 lightOrigin, Color lightColor, Color shadowColor,
        float beamStrength, float dustStrength, float halftoneStrength, float vignetteStrength, float seed)
    {
        LightOrigin = lightOrigin;
        LightColor = lightColor;
        ShadowColor = shadowColor;
        BeamStrength = Math.Clamp(beamStrength, 0f, 1f);
        DustStrength = Math.Clamp(dustStrength, 0f, 1f);
        HalftoneStrength = Math.Clamp(halftoneStrength, 0f, 1f);
        VignetteStrength = Math.Clamp(vignetteStrength, 0f, 1f);
        Seed = seed;
        dirty = true;
        ApplyUniforms();
    }

    public void SetDetail(int detail)
    {
        int clamped = Math.Clamp(detail, 0, 2);
        if (Detail == clamped && !dirty) return;
        Detail = clamped;
        dirty = true;
        ApplyUniforms();
    }

    /// <summary>Host drives all time and pointer coordinates; passing unchanged time freezes motion.</summary>
    public void UpdateSurface(double time, Vector2 normalizedPointer)
    {
        double clampedTime = Math.Max(0, time);
        Vector2 clampedPointer = Detail == 2 ? normalizedPointer.Clamp(-Vector2.One, Vector2.One) : Vector2.Zero;
        if (!dirty && Math.Abs(clampedTime - ClockTime) < 1e-6 && clampedPointer.IsEqualApprox(pointer))
            return;
        ClockTime = clampedTime;
        pointer = clampedPointer;
        dirty = true;
        ApplyUniforms();
    }

    private void ApplyUniforms()
    {
        if (!IsInsideTree()) return;
        shaderMaterial ??= UiShaders.Material("scene_atmosphere");
        Material = shaderMaterial;
        shaderMaterial.SetShaderParameter("clock", Detail == 2 ? ClockTime : 0.0);
        shaderMaterial.SetShaderParameter("detail", Detail);
        shaderMaterial.SetShaderParameter("pointer", Detail == 2 ? pointer : Vector2.Zero);
        shaderMaterial.SetShaderParameter("light_origin", LightOrigin);
        shaderMaterial.SetShaderParameter("light_color", LightColor);
        shaderMaterial.SetShaderParameter("shadow_color", ShadowColor);
        shaderMaterial.SetShaderParameter("beam_strength", BeamStrength);
        shaderMaterial.SetShaderParameter("dust_strength", DustStrength);
        shaderMaterial.SetShaderParameter("halftone_strength", HalftoneStrength);
        shaderMaterial.SetShaderParameter("vignette_strength", VignetteStrength);
        shaderMaterial.SetShaderParameter("seed", Seed);
        dirty = false;
    }

    public override void _EnterTree()
    {
        dirty = true;
        ApplyUniforms();
    }

    private void ReleaseMaterial()
    {
        Material = null;
        shaderMaterial?.Dispose();
        shaderMaterial = null;
        dirty = true;
    }

    public override void _ExitTree() => ReleaseMaterial();
}
