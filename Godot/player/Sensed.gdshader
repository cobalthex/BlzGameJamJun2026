shader_type spatial;

global uniform vec3 sense_sphere_center = vec3(0.0);
global uniform float sense_sphere_trailing_radius;
global uniform float sense_sphere_leading_radius;
// Controls how quickly the sphere fades -- TODO: split inner/outer?

uniform float sense_sphere_trailing_fade = 15.0;
uniform float sense_sphere_leading_fade = 2.0;

uniform vec3 flat_color : source_color;
uniform sampler2D albedo_tex : source_color;

uniform vec2 uv_scale = vec2(1.0, 1.0);

uniform vec3 edge_color : source_color = vec3(0.4, 0.9, 1.0);
uniform float edge_radius = 10.0;

float calc_sense_mask(float dist)
{
	//return dist >= sense_sphere_trailing_radius && dist <= sense_sphere_leading_radius ? 1.0 : 0.0;

    float tail = smoothstep(
        sense_sphere_trailing_radius,
        sense_sphere_trailing_radius + sense_sphere_trailing_fade,
        dist);

    float head = 1.0 - smoothstep(
        sense_sphere_leading_radius - sense_sphere_leading_fade,
        sense_sphere_leading_radius,
        dist);

    return tail * head;
}

varying vec3 world_pos;

void vertex()
{
    world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	UV *= uv_scale;
}

void fragment()
{
    float dist_to_sense_sphere = distance(world_pos, sense_sphere_center);
    float tail = smoothstep(
        sense_sphere_trailing_radius,
        sense_sphere_trailing_radius + sense_sphere_trailing_fade,
        dist_to_sense_sphere);

    float head = 1.0 - smoothstep(
        sense_sphere_leading_radius - sense_sphere_leading_fade,
        sense_sphere_leading_radius,
        dist_to_sense_sphere);
    float sense_mask = tail * head;

    // todo: only draw on edges?
    // todo: edge radius should hide under trailing radius near finish
    float edge_overlay = sense_mask * smoothstep(
        sense_sphere_leading_radius - edge_radius,
        sense_sphere_leading_radius,
        dist_to_sense_sphere);

    vec4 tex = texture(albedo_tex, UV);
    EMISSION = mix(flat_color, edge_color, edge_overlay) * (1.0 - sense_mask);
    ALBEDO = mix(flat_color, tex.rgb, sense_mask);
}

void light()
{
    float dist_to_sense_sphere = distance(world_pos, sense_sphere_center);
    float tail = smoothstep(
        sense_sphere_trailing_radius,
        sense_sphere_trailing_radius + sense_sphere_trailing_fade,
        dist_to_sense_sphere);

    float head = 1.0 - smoothstep(
        sense_sphere_leading_radius - sense_sphere_leading_fade,
        sense_sphere_leading_radius,
        dist_to_sense_sphere);
    float sense_mask = tail * head;
        
    vec3 diffuse =
        ALBEDO *
        LIGHT_COLOR.rgb *
        ATTENUATION *
        max(dot(NORMAL, LIGHT), 0.0);
    DIFFUSE_LIGHT += diffuse * sense_mask;
}