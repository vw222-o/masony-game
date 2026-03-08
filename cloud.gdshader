shader_type spatial;

uniform vec4 cloud_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform vec4 shadow_color : source_color = vec4(0.85, 0.88, 0.92, 1.0);
uniform float shadow_threshold : hint_range(-1.0, 1.0) = 0.0;
uniform float shadow_softness : hint_range(0.0, 1.0) = 0.02;
uniform float rim_intensity : hint_range(0.0, 1.0) = 0.3;
uniform float rim_threshold : hint_range(0.0, 1.0) = 0.5;

void fragment() {
    ALBEDO = vec3(1.0);
}

void light() {
    // N.L
    float NdotL = dot(NORMAL, LIGHT);
    float shadow_mask = smoothstep(shadow_threshold - shadow_softness, shadow_threshold + shadow_softness, NdotL);
    
    float NdotV = dot(NORMAL, VIEW);
    float rim_mask = pow(1.0 - clamp(NdotV, 0.0, 1.0), 3.0);
    float rim = step(rim_threshold, rim_mask) * rim_intensity;
    
    // shadow + rim
    vec3 final_color = mix(shadow_color.rgb, cloud_color.rgb, shadow_mask);
    final_color = mix(final_color, cloud_color.rgb, rim);
    
    // light color and attenuation
    DIFFUSE_LIGHT += final_color * ATTENUATION * LIGHT_COLOR;
}
