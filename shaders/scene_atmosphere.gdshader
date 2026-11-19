shader_type canvas_item;
render_mode unshaded;

uniform float clock = 0.0;
uniform int detail = 2;
uniform vec2 pointer = vec2(0.0);
uniform vec2 light_origin = vec2(0.5, 0.08);
uniform vec4 light_color : source_color = vec4(0.39, 0.89, 0.83, 1.0);
uniform vec4 shadow_color : source_color = vec4(0.04, 0.06, 0.11, 1.0);
uniform float beam_strength = 0.22;
uniform float dust_strength = 0.28;
uniform float halftone_strength = 0.07;
uniform float vignette_strength = 0.34;
uniform float seed = 17.0;

float hash21(vec2 p) {
    p = fract(p * vec2(123.34, 456.21) + seed * 0.173);
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}

float mote_layer(vec2 uv, float scale, vec2 velocity, float time_val, float layer_id) {
    vec2 p = uv * vec2(scale * 1.7778, scale) - velocity * time_val + vec2(layer_id * 11.3, seed * 0.7);
    vec2 cell = floor(p);
    vec2 local = fract(p) - 0.5;
    float rnd = hash21(cell + vec2(layer_id * 37.0, 0.0));
    if (rnd < 0.72) {
        return 0.0;
    }
    vec2 jitter = vec2(hash21(cell + 13.7), hash21(cell + 71.9)) - 0.5;
    vec2 offset = jitter * 0.62;
    offset.x += sin(time_val * (0.45 + rnd * 0.5) + rnd * 6.2831) * 0.08;
    float d = length(local - offset);
    float radius = mix(0.035, 0.085, hash21(cell + 5.1));
    float sparkle = smoothstep(radius, radius * 0.12, d);
    float twinkle = 0.55 + 0.45 * sin(time_val * (1.2 + rnd * 1.8) + rnd * 18.0);
    return sparkle * twinkle;
}

void fragment() {
    vec2 uv = UV;
    float active_motion = detail == 2 ? 1.0 : 0.0;
    float t = clock * active_motion;
    vec2 p_shift = pointer * 0.018 * active_motion;

    // Soft elliptical vignette framing the composition without obscuring central UI.
    vec2 centered = (uv - vec2(0.5)) * vec2(1.0, 1.06);
    float edge_dist = length(centered);
    float vignette = smoothstep(0.36, 0.86, edge_dist) * vignette_strength;

    // Subtle graphic-novel halftone print texture confined to deep outer corners.
    float halftone = 0.0;
    if (detail >= 1 && halftone_strength > 0.001) {
        vec2 grid = fract(uv * vec2(160.0, 90.0)) - 0.5;
        float dot_mask = 1.0 - smoothstep(0.16, 0.30, length(grid));
        float corner_zone = smoothstep(0.46, 0.84, edge_dist);
        halftone = dot_mask * corner_zone * halftone_strength;
    }

    // Directional volumetric light cone and gentle atmospheric rays.
    float beam = 0.0;
    if (detail >= 1 && beam_strength > 0.001) {
        vec2 origin = light_origin + p_shift;
        vec2 delta = (uv - origin) * vec2(1.45, 1.0);
        float dist = length(delta);
        float radial = exp(-dist * 1.85);
        float angle = atan(delta.y, delta.x);
        float rays = 0.62
            + 0.22 * sin(angle * 7.0 + seed + t * 0.22)
            + 0.16 * sin(angle * 13.0 - seed * 0.6 - t * 0.15);
        float breath = 0.90 + 0.10 * sin(t * 0.85 + seed);
        beam = radial * max(rays, 0.15) * breath * beam_strength;
    }

    // Drifting light-caught atmospheric dust motes (Full detail only).
    float dust = 0.0;
    if (detail == 2 && dust_strength > 0.001) {
        vec2 uv_parallax = uv + p_shift * 0.65;
        float m1 = mote_layer(uv_parallax, 9.0, vec2(0.06, -0.14), t, 1.0);
        float m2 = mote_layer(uv_parallax, 15.0, vec2(-0.04, -0.22), t, 2.0);
        float vertical_fade = smoothstep(0.0, 0.18, uv.y) * (1.0 - smoothstep(0.75, 1.0, uv.y));
        dust = (m1 * 0.65 + m2 * 0.45) * (0.45 + 0.55 * vertical_fade) * dust_strength;
    }

    float dark_a = clamp(vignette + halftone, 0.0, 0.82);
    float bright_a = clamp(beam + dust, 0.0, 0.72);
    float out_a = 1.0 - (1.0 - dark_a) * (1.0 - bright_a);

    vec3 out_rgb = vec3(0.0);
    if (out_a > 0.0005) {
        vec3 dust_tint = mix(light_color.rgb, vec3(1.0, 0.98, 0.92), clamp(dust / max(beam + dust, 0.001) * 0.55, 0.0, 0.55));
        out_rgb = (shadow_color.rgb * dark_a * (1.0 - bright_a) + dust_tint * bright_a) / out_a;
    }
    COLOR = vec4(out_rgb * COLOR.rgb, out_a * COLOR.a);
}
