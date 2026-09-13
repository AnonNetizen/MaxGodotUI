// Procedural background contract. No texture inputs or implicit engine time.
uniform float clock = 0.0;
uniform int detail = 2;
uniform vec2 display_size = vec2(1920.0, 1080.0);
uniform vec2 pointer = vec2(0.0);
uniform vec2 center = vec2(0.5);
uniform vec4 base_color : source_color = vec4(0.025, 0.025, 0.025, 1.0);
uniform vec4 primary_color : source_color = vec4(0.5, 0.5, 0.5, 1.0);
uniform vec4 secondary_color : source_color = vec4(0.2, 0.2, 0.2, 1.0);
uniform float flow_speed = 1.0;
uniform float intensity = 1.0;
uniform float seed = 17.0;
varying vec4 modulation;
void vertex() { modulation = COLOR; }
float bg_time() { return detail == 2 ? clock * flow_speed : 0.0; }
vec2 bg_point(vec2 uv) {
    return (uv - center) * vec2(display_size.x / display_size.y, 1.0) + pointer * 0.008;
}
mat2 bg_rotate(float a) { return mat2(vec2(cos(a), sin(a)), vec2(-sin(a), cos(a))); }
float bg_hash(vec2 p) {
    vec3 q = fract(vec3(p.xyx) * vec3(0.1031, 0.1030, 0.0973));
    q += dot(q, q.yzx + 33.33 + seed * 0.013);
    return fract((q.x + q.y) * q.z);
}
float bg_noise(vec2 p) {
    vec2 cell = floor(p), f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(mix(bg_hash(cell), bg_hash(cell + vec2(1, 0)), f.x),
        mix(bg_hash(cell + vec2(0, 1)), bg_hash(cell + vec2(1, 1)), f.x), f.y);
}
float bg_cloud(vec2 p) {
    return bg_noise(p) * 0.57 + bg_noise(p * 2.03 + 13.7) * 0.28 + bg_noise(p * 4.11 + 31.2) * 0.15;
}
float bg_line(float distance_to_line, float width) {
    float aa = max(fwidth(distance_to_line), 0.00025);
    return 1.0 - smoothstep(width, width + aa, abs(distance_to_line));
}
float bg_stars(vec2 p, float density) {
    vec2 cell = floor(p * density), local = fract(p * density);
    vec2 offset = vec2(bg_hash(cell), bg_hash(cell + 19.7));
    float radius = length(local - (0.15 + offset * 0.7));
    // Different cells contain different stars; differentiating radius across the
    // cell boundary would turn that discontinuity into bright horizontal seams.
    float aa = max(density / display_size.y, 0.001);
    return (1.0 - smoothstep(0.01, 0.01 + aa, radius)) * step(0.80, bg_hash(cell + 71.0));
}
vec3 bg_base(vec2 uv) {
    float glow = max(0.0, 1.0 - distance(uv, center));
    return base_color.rgb * (0.8 + 0.6 * (1.0 - uv.y)) + primary_color.rgb * glow * 0.035;
}
vec4 bg_finish(vec3 color, vec2 uv) {
    float vignette = 1.0 - 0.32 * smoothstep(0.25, 0.85, length(uv - vec2(0.5)));
    return vec4(color * vignette, 1.0) * modulation;
}
