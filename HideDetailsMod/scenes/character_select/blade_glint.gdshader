shader_type canvas_item;

// --- Color Filter Settings ---
uniform vec3 blade_color_target : source_color = vec3(0.1, 0.4, 0.8);
uniform float color_tolerance : hint_range(0.01, 1.0) = 0.35;
uniform float white_brightness_cutoff : hint_range(0.5, 1.0) = 0.85;

// --- Combined Angle & Direction Control ---
// Tilting this completely rotates the line AND dictates its travel path (0 = Left to Right, 180 = Right to Left, etc.)
uniform float shine_angle : hint_range(-180.0, 180.0) = 0.0;

// --- Glint Behavior Settings ---
uniform float shine_speed : hint_range(0.1, 5.0) = 1.0;
uniform float shine_sharpness : hint_range(1.0, 50.0) = 15.0;
uniform float shine_intensity : hint_range(0.1, 5.0) = 1.2;

void fragment() {
    vec4 tex_color = texture(TEXTURE, UV);
    
    // 1. Isolate the blue/white areas of the blade artwork
    float brightness = dot(tex_color.rgb, vec3(0.299, 0.587, 0.114));
    float color_distance = distance(tex_color.rgb, blade_color_target);
    
    float is_blue = smoothstep(color_tolerance, color_tolerance - 0.05, color_distance);
    float is_white = smoothstep(white_brightness_cutoff, white_brightness_cutoff + 0.05, brightness);
    float blade_mask = max(is_blue, is_white) * step(0.01, tex_color.a);
    
    // 2. Convert your angle to radians
    float angle_rad = radians(shine_angle);
    
    // 3. Project the UV grid along that custom angle vector
    // This calculation links the movement track directly to the line tilt
    float projected_uv = UV.x * cos(angle_rad) + UV.y * sin(angle_rad);
    
    // 4. Generate the continuous wave sweep line
    float line_position = sin((projected_uv * 3.14159) - (TIME * shine_speed));
    float glint_line = pow(max(0.0, 1.0 - abs(line_position)), shine_sharpness);
    
    // 5. Apply the glint brightness to the masked blade area
    vec3 final_shine = vec3(glint_line * shine_intensity * blade_mask);
    
    COLOR = vec4(tex_color.rgb + final_shine, tex_color.a);
}
