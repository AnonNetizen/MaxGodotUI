shader_type canvas_item;
render_mode unshaded;
#include "background_common.gdshaderinc"
void fragment() {
    vec3 color = bg_base(UV);
    if (detail > 0) {
        vec2 p = bg_point(UV);
        float t = bg_time();
        float edge = smoothstep(0.22, 0.76, length(p * vec2(0.7, 1.1)));
        float wave = p.y + sin(p.x * 3.0 + t * 0.06) * 0.14;
        float lines = pow(max(0.0, cos(wave * 39.0 + p.x * 2.4)), 24.0);
        float field = bg_cloud(p * 2.4 + vec2(t * 0.009, -t * 0.012));
        float rings = bg_line(length(p * vec2(0.7, 1.0)) - 0.45, 0.0008);
        color += intensity * edge * (primary_color.rgb * (lines * 0.12 + rings * 0.25)
            + secondary_color.rgb * field * 0.20);
        color += primary_color.rgb * bg_stars(p, 18.0) * edge * 0.16;
    }
    COLOR = bg_finish(color, UV);
}
