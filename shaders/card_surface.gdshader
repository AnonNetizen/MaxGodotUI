shader_type canvas_item;
uniform float clock = 0.0;
uniform vec2 pointer = vec2(0.5);
uniform float strength = 0.0;
uniform float reveal = 1.0;
uniform float impact = 0.0;
uniform vec4 art_region = vec4(0.0, 0.0, 1.0, 1.0);
uniform float dissolve = 0.0;
uniform bool antialias_edges = true;
varying vec4 modulation;
void vertex() { modulation = COLOR; }
void fragment() {
    vec4 tex = texture(TEXTURE, UV) * modulation;
    vec2 local_uv = (UV - art_region.xy) / art_region.zw;
    vec2 edge = smoothstep(vec2(0.0), max(fwidth(local_uv), vec2(0.000001)), min(local_uv, vec2(1.0) - local_uv));
    float sweep = local_uv.x * 0.72 + local_uv.y * 0.28 - pointer.x * 0.55 - pointer.y * 0.16;
    float band = exp(-pow((sweep - 0.23) * 8.0, 2.0));
    vec3 sheen = 0.5 + 0.5 * cos(vec3(0.0, 1.8, 3.4) + sweep * 7.0 + clock * 0.3);
    tex.rgb += sheen * band * strength * 0.24;
    float scan = smoothstep(local_uv.y - 0.035, local_uv.y + 0.035, reveal * 1.08);
    tex.rgb += vec3(0.25, 0.85, 0.94) * exp(-abs(local_uv.y - reveal) * 75.0) * step(reveal, 0.99) * 0.5;
    tex.rgb = mix(tex.rgb, vec3(0.8, 0.95, 1.0), impact * 0.5);
    float cell = fract(sin(dot(floor(local_uv * 38.0), vec2(12.9898, 78.233))) * 43758.5453);
    tex.a *= (antialias_edges ? edge.x * edge.y : 1.0) * scan * (1.0 - smoothstep(cell, cell + 0.08, dissolve));
    COLOR = tex;
}
