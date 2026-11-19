shader_type canvas_item;
uniform float clock = 0.0;
uniform vec2 pointer = vec2(0.5);
uniform float strength = 0.0;
uniform float reveal = 1.0;
uniform float impact = 0.0;
uniform vec4 art_region = vec4(0.0, 0.0, 1.0, 1.0);
uniform float dissolve = 0.0;
uniform bool antialias_edges = true;
uniform vec4 dissolve_ink : source_color = vec4(1.0, 0.44, 0.28, 1.0);
varying vec4 modulation;
void vertex() { modulation = COLOR; }
void fragment() {
    vec4 tex = texture(TEXTURE, UV) * modulation;
    vec2 local_uv = (UV - art_region.xy) / art_region.zw;
    vec2 edge = smoothstep(vec2(0.0), max(fwidth(local_uv), vec2(0.000001)), min(local_uv, vec2(1.0) - local_uv));
    float sweep = local_uv.x * 0.72 + local_uv.y * 0.28 - pointer.x * 0.55 - pointer.y * 0.16;
    float band = exp(-pow((sweep - 0.23) * 8.5, 2.0));
    float secondary = exp(-pow((sweep + 0.16) * 14.0, 2.0)) * 0.45;
    vec3 sheen = 0.55 + 0.45 * cos(vec3(0.0, 1.7, 3.2) + sweep * 7.5 + clock * 0.35);
    float rim_catch = (1.0 - smoothstep(0.0, 0.09, min(min(local_uv.x, 1.0 - local_uv.x), min(local_uv.y, 1.0 - local_uv.y)))) * 0.14;
    tex.rgb += sheen * (band + secondary) * strength * 0.26 + vec3(0.75, 0.94, 1.0) * rim_catch * strength;
    float scan = smoothstep(local_uv.y - 0.032, local_uv.y + 0.032, reveal * 1.08);
    float scan_line = exp(-abs(local_uv.y - reveal) * 85.0) * step(reveal, 0.99);
    tex.rgb += vec3(0.32, 0.92, 0.96) * scan_line * 0.65;
    tex.rgb = mix(tex.rgb, vec3(1.0, 0.96, 0.88), impact * 0.58);
    vec2 rot_uv = vec2(local_uv.x * 0.82 - local_uv.y * 0.57, local_uv.x * 0.57 + local_uv.y * 0.82);
    vec2 shard_cell = floor(rot_uv * vec2(16.0, 22.0));
    float shard = fract(sin(dot(shard_cell, vec2(127.1, 311.7))) * 43758.5453);
    float gradient_bias = local_uv.y * 0.22;
    float threshold = shard * 0.78 + gradient_bias;
    float dissolved = smoothstep(threshold - 0.05, threshold + 0.04, dissolve * 1.12);
    float burn_edge = smoothstep(0.0, 0.14, dissolve) * (1.0 - dissolved) * smoothstep(threshold - 0.16, threshold - 0.01, dissolve * 1.12);
    tex.rgb = mix(tex.rgb, dissolve_ink.rgb * 1.35, burn_edge * 0.92);
    tex.a *= (antialias_edges ? edge.x * edge.y : 1.0) * scan * (1.0 - dissolved);
    COLOR = tex;
}
