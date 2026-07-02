shader_type spatial;

global uniform vec3 sense_sphere_center = vec3(0.0);
global uniform float sense_sphere_trailing_radius;
global uniform float sense_sphere_leading_radius;
// Controls how quickly the sphere fades -- TODO: split inner/outer?

uniform float sense_sphere_trailing_fade = 15.0;
uniform float sense_sphere_leading_fade = 2.0;

uniform vec3 color : source_color;
uniform vec3 illumination_color : source_color = vec3(0.0, 0.6, 1.0);
uniform float illumination_strength = 0.5;

uniform float band_spacing = 3.0;
uniform float band_strength = 0.35;

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
    // float edge_overlay = sense_mask * smoothstep(
    //     sense_sphere_leading_radius - edge_radius,
    //     sense_sphere_leading_radius,
    //     dist_to_sense_sphere);
    // EMISSION = mix(flat_color, edge_color, edge_overlay) * (1.0 - sense_mask);

    float stripes = 1.0 - band_strength * abs(fract(dist_to_sense_sphere / band_spacing) - 1.0) * 5.0;

    // stripes = clamp(stripes, 0.0, 1.0);
    ALBEDO = mix(color, illumination_color, sense_mask * stripes * illumination_strength);
    EMISSION = color;// * (1.0 - sense_mask);
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