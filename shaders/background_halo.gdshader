shader_type canvas_item;
render_mode unshaded;
#include "background_common.gdshaderinc"
void fragment() {
    vec3 color = bg_base(UV);
    if (detail > 0) {
        vec2 p = bg_point(UV);
        float t = bg_time(), r = length(p);
        float angle = atan(p.y, p.x);
        float turbulence = bg_cloud(p * 5.0 + vec2(t * 0.02, -t * 0.013));
        float ring = exp(-abs(r - 0.34 - 0.025 * sin(angle * 4.0 + turbulence * 3.0 + t * 0.12)) * 25.0);
        float spread = pow(max(0.0, cos(r * 14.0 - t * 0.18 + turbulence * 2.0)), 6.0) * exp(-r * 1.8);
        color += intensity * (primary_color.rgb * ring * 0.40 + secondary_color.rgb * spread * 0.20);
        color += primary_color.rgb * bg_stars(p + vec2(0, t * 0.001), 21.0) * 0.34;
    }
    COLOR = bg_finish(color, UV);
}
