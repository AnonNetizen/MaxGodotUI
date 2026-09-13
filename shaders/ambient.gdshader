shader_type canvas_item;
uniform float clock = 0.0;
uniform float strength = 1.0;
uniform vec2 pointer = vec2(0.0);
varying vec4 modulation;
void vertex() { modulation = COLOR; }
void fragment() {
    vec2 uv = clamp(UV * 0.994 + 0.003 + pointer * 0.002 * strength, vec2(0.0), vec2(1.0));
    vec4 tex = texture(TEXTURE, uv) * modulation;
    float flow = pow(max(0.0, sin(uv.x * 5.0 + uv.y * 3.0 - clock * 0.16)), 10.0);
    tex.rgb += vec3(0.10, 0.25, 0.30) * flow * 0.075 * strength;
    COLOR = tex;
}
