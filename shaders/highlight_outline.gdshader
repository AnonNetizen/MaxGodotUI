shader_type canvas_item;
render_mode unshaded;

uniform vec2 rect_origin = vec2(0.0);
uniform vec2 rect_size = vec2(184.0, 260.0);
uniform float radius = 4.0;
uniform vec4 ink : source_color = vec4(1.0);
uniform float line_width = 2.0;
uniform float glow_width = 8.0;
uniform float glow_opacity = 0.3;
uniform float corner_length = 0.0;
uniform float brightness = 1.0;
varying vec2 local_position;

void vertex() { local_position = VERTEX; }

void fragment() {
    vec4 tint = COLOR;
    vec2 half_size = rect_size * 0.5;
    vec2 p = local_position - rect_origin - half_size;
    float r = clamp(radius, 0.0, min(half_size.x, half_size.y));
    vec2 q = abs(p) - half_size + r;
    float distance = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
    float aa = max(fwidth(distance), 0.35);
    // Everything lies on or outside the edge: text and illustration remain untouched.
    float exterior = smoothstep(-aa * 0.5, aa * 0.5, distance);
    float rim = 1.0 - smoothstep(line_width - aa, line_width + aa, distance);
    float halo = 0.0;
    if (glow_width > 0.0) {
        float t = clamp(max(distance - line_width, 0.0) / glow_width, 0.0, 1.0);
        halo = pow(1.0 - t, 2.4) * glow_opacity;
    }
    vec2 corner_distance = half_size - abs(p);
    float at_corner = 1.0 - smoothstep(corner_length - aa, corner_length + aa, max(corner_distance.x, corner_distance.y));
    float bracket = (1.0 - smoothstep(0.65, 0.65 + aa, abs(distance - line_width - 2.0))) * at_corner;
    bracket *= step(0.1, corner_length);
    float alpha = max(max(rim * 0.88, halo), bracket) * exterior * ink.a * brightness;
    vec3 color = mix(ink.rgb, vec3(1.0), bracket * 0.3);
    COLOR = vec4(color * tint.rgb, alpha * tint.a);
}
