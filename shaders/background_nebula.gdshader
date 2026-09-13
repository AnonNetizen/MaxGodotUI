shader_type canvas_item;
render_mode unshaded;
#include "background_common.gdshaderinc"
void fragment() {
    vec3 color = bg_base(UV);
    if (detail > 0) {
        vec2 p = bg_point(UV);
        float t = bg_time(), r = length(p);
        vec2 q = bg_rotate(1.4 * exp(-r * 1.6) + 0.12 * sin(t * 0.11)) * p;
        float cloud = bg_cloud(q * 3.2 + vec2(-t * 0.035, t * 0.017));
        float curl = sin(q.y * 8.0 + cloud * 6.2 - t * 0.12 + q.x * 2.0);
        float band = smoothstep(-0.8, 0.7, curl) * exp(-r * 0.8);
        float vein = pow(max(0.0, 1.0 - abs(curl)), 5.0) * smoothstep(0.2, 0.75, cloud);
        vec3 gas = mix(secondary_color.rgb, primary_color.rgb, smoothstep(0.3, 0.8, cloud));
        color += intensity * (gas * band * 0.38 + primary_color.rgb * vein * 0.14);
        vec2 orbit = bg_rotate(-0.32) * p;
        float arc = bg_line(length(orbit * vec2(1.0, 1.6)) - 0.57, 0.0007);
        color += primary_color.rgb * arc * 0.13 * smoothstep(-0.5, 0.5, p.x);
        color += mix(primary_color.rgb, vec3(1.0), 0.65) * bg_stars(p + vec2(t * 0.001, 0), 23.0) * 0.42;
    }
    COLOR = bg_finish(color, UV);
}
