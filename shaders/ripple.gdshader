shader_type canvas_item;
uniform float progress = 0.0;
uniform vec4 ink : source_color = vec4(0.39, 0.86, 0.92, 1.0);
uniform float fractured = 0.0;
void fragment() {
    vec2 p = UV - vec2(0.5);
    float dist = length(p);
    float r = 0.06 + progress * 0.42;
    float ring = 1.0 - smoothstep(0.008, 0.028, abs(dist - r));
    float inner_ring = (1.0 - smoothstep(0.005, 0.018, abs(dist - r * 0.72))) * 0.45;
    float glow = exp(-abs(dist - r) * 18.0) * 0.28;
    float gaps = mix(1.0, step(0.0, sin(atan(p.y, p.x) * 6.0)), fractured);
    float alpha = (ring + inner_ring + glow) * gaps * (1.0 - progress);
    vec3 color = mix(ink.rgb, vec3(1.0, 0.98, 0.92), ring * 0.35);
    COLOR = vec4(color, clamp(alpha, 0.0, 1.0) * ink.a);
}
