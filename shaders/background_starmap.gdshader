shader_type canvas_item;
render_mode unshaded;
#include "background_common.gdshaderinc"
void fragment() {
    vec3 color = bg_base(UV);
    if (detail > 0) {
        vec2 p = bg_point(UV);
        float t = bg_time();
        vec2 grid = abs(fract(p * 7.0 + 0.5) - 0.5) / 7.0;
        float lines = max(bg_line(grid.x, 0.00035), bg_line(grid.y, 0.00035));
        vec2 q = bg_rotate(-0.4) * p;
        float r = length(q * vec2(1.0, 1.45));
        float orbits = bg_line(r - 0.27, 0.0007) + bg_line(r - 0.53, 0.0007) + bg_line(r - 0.82, 0.0007);
        float sweep = pow(max(0.0, cos(p.y * 3.2 - t * 0.16)), 18.0);
        float nodes = bg_stars(p, 13.0);
        float quiet = 0.3 + 0.7 * smoothstep(0.2, 0.75, length(UV - vec2(0.5)));
        color += intensity * quiet * (primary_color.rgb * (lines * 0.07 + orbits * 0.23 + nodes * 0.5)
            + secondary_color.rgb * sweep * 0.10);
    }
    COLOR = bg_finish(color, UV);
}
