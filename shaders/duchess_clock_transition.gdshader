shader_type canvas_item;
render_mode unshaded;
uniform float threshold : hint_range(0.0, 1.0) = 0.0;
uniform bool revealing = false;

float line_distance(vec2 p, vec2 b) {
    return length(p-b*clamp(dot(p,b)/dot(b,b), -0.12, 1.0));
}
void fragment() {
    vec2 size = 1.0 / SCREEN_PIXEL_SIZE;
    vec2 p = (UV-0.5)*size/min(size.x,size.y);
    float r = length(p);
    float aa = 1.5/min(size.x,size.y);
    float phase = revealing ? 1.0-threshold : threshold;
    float opening = smoothstep(0.48,1.0,phase)*(length(size/min(size.x,size.y)*0.5)+0.04);
    float cover = revealing ? smoothstep(opening-aa,opening+aa,r) : 1.0-smoothstep(opening-aa,opening+aa,r);
    float angle = atan(p.x,-p.y);
    float tick = abs(mod(angle+0.05235988,0.10471976)-0.05235988);
    float major = abs(mod(angle+0.26179939,0.52359878)-0.26179939);
    float ring = 1.0-smoothstep(0.002,0.002+aa,abs(r-0.285));
    float inner = 1.0-smoothstep(0.001,0.001+aa,abs(r-0.245));
    float marks = (1.0-smoothstep(0.004,0.010,tick))*step(0.259,r)*step(r,0.278);
    marks = max(marks,(1.0-smoothstep(0.011,0.017,major))*step(0.249,r)*step(r,0.279));
    float rotation = phase*12.5663706;
    vec2 minute = vec2(sin(rotation),-cos(rotation))*0.225;
    vec2 hour = vec2(sin(rotation*0.25+0.8),-cos(rotation*0.25+0.8))*0.15;
    float hands = 1.0-smoothstep(0.003,0.003+aa,line_distance(p,minute));
    hands = max(hands,1.0-smoothstep(0.005,0.005+aa,line_distance(p,hour)));
    hands = max(hands,1.0-smoothstep(0.009,0.009+aa,r));
    float visibility = smoothstep(0.0,0.14,phase)*(1.0-smoothstep(0.68,0.94,phase));
    float clock = max(max(ring,inner*0.5),max(marks,hands))*visibility;
    vec3 ink = mix(vec3(0.72,0.63,0.43),vec3(0.282353,0.725490,0.929412),hands);
    COLOR = vec4(ink*clock,max(cover,clock));
    if (threshold <= 0.0) { COLOR = vec4(0.0); }
    if (threshold >= 1.0) { COLOR = vec4(0.0,0.0,0.0,1.0); }
}
