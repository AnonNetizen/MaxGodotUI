shader_type canvas_item;

// A half-screen-pixel coverage ramp smooths edges while keeping thin rules bright.
// This material has no per-node parameters and is shared by all geometric rules.
void fragment() {
    vec2 pixel = max(fwidth(UV) * 0.5, vec2(0.000001));
    vec2 coverage = smoothstep(vec2(0.0), pixel, min(UV, vec2(1.0) - UV));
    COLOR.a *= coverage.x * coverage.y;
}
