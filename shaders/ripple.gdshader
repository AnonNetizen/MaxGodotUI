shader_type canvas_item;
uniform float progress = 0.0;
uniform vec4 ink : source_color = vec4(0.39, 0.86, 0.92, 1.0);
uniform float fractured = 0.0;
void fragment() {
    vec2 p = UV - vec2(0.5);
    float ring = 1.0 - smoothstep(0.008, 0.025, abs(length(p) - (0.06 + progress * 0.40)));
    float gaps = mix(1.0, step(0.0, sin(atan(p.y, p.x) * 6.0)), fractured);
    COLOR = vec4(ink.rgb, ink.a * ring * gaps * (1.0 - progress));
}
