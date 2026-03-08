shader_type spatial;
//render_mode cull_disabled;

uniform vec3 cloud_color : source_color = vec3(1.0, 1.0, 1.0);
uniform vec3 shadow_color : source_color = vec3(0.8, 0.8, 0.85);

void fragment() {
	ALBEDO = cloud_color;
}

void light() {
	vec3 up = (vec4(0., 1., 0., 0.) * INV_VIEW_MATRIX).xyz;
	float NdotL = dot(NORMAL, normalize(up));
	float is_light = step(0.1, NdotL);
	vec3 final = mix(shadow_color, cloud_color, is_light);

	DIFFUSE_LIGHT += final / ATTENUATION * LIGHT_COLOR / PI;
	ALPHA = 0.2;
}
