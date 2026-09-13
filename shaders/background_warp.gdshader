shader_type canvas_item;
render_mode unshaded;
#include "background_common.gdshaderinc"
void fragment() {
    vec3 color = bg_base(UV);
    if (detail > 0) {
        vec2 p = bg_point(UV);
        float t = bg_time(), r = length(p);
        float angle = atan(p.y, p.x);
        float arms = pow(max(0.0, sin(angle * 7.0 + r * 3.0 - t * 0.12)), 12.0);
        float rings = pow(max(0.0, cos(r * 21.0 - t * 0.32)), 16.0);
        float mist = bg_cloud(p * 3.5 + vec2(0.0, t * 0.022));
        float hollow = smoothstep(0.07, 0.35, r);
        vec3 ribbon = mix(secondary_color.rgb, primary_color.rgb, mist);
        color += intensity * hollow * (ribbon * (arms * 0.28 + rings * 0.19) + secondary_color.rgb * mist * 0.14);
        color += primary_color.rgb * bg_line(r - 0.21, 0.0012) * 0.25;
        color += primary_color.rgb * bg_stars(p, 20.0) * 0.27;
    }
    COLOR = bg_finish(color, UV);
}
