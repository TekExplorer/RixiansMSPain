shader_type canvas_item;

// --- Direction Configurations (in degrees) ---
// Angle when sweeping through the left side (e.g., 15.0 degrees for slightly up)
uniform float angle_left_side : hint_range(-90.0, 90.0) = 15.0;
// Angle when sweeping through the right side (e.g., -15.0 degrees for slightly down)
uniform float angle_right_side : hint_range(-90.0, 90.0) = -15.0;

// --- Positioning & Midpoint ---
// The exact horizontal spot where the angle shifts (0.0 = Left edge, 0.5 = Center, 1.0 = Right edge)
uniform float shift_midpoint : hint_range(0.0, 1.0) = 0.5;
// How smoothly the angle blends at the transition boundary (0.01 = sharp snap, 0.2 = soft curve)
uniform float shift_smoothing : hint_range(0.01, 0.5) = 0.05;

// --- Glint Settings ---
uniform float shine_speed : hint_range(0.1, 5.0) = 1.0;
uniform float shine_sharpness : hint_range(1.0, 50.0) = 15.0;
uniform float shine_intensity : hint_range(0.1, 5.0) = 1.0;
uniform float brightness_threshold : hint_range(0.0, 1.0) = 0.5;

void fragment() {
    vec4 tex_color = texture(TEXTURE, UV);
    float brightness = dot(tex_color.rgb, vec3(0.299, 0.587, 0.114));
    
    // 1. Determine where we are on the texture horizontally to figure out the blend factor
    // Smoothstep creates a clean interpolation zone right around our configurable midpoint
    float side_blend = smoothstep(shift_midpoint - shift_smoothing, shift_midpoint + shift_smoothing, UV.x);
    
    // 2. Mix the two angles together based on the current pixel's X coordinate
    float current_angle = mix(angle_left_side, angle_right_side, side_blend);
    float angle_rad = radians(current_angle);
    
    // 3. Project the coordinates along our localized, shifting angle vector
    float projected_uv = UV.x * cos(angle_rad) + UV.y * sin(angle_rad);
    
    // 4. Generate the continuous left-to-right sweep line
    float line_position = sin((projected_uv * 3.14159) - (TIME * shine_speed));
    float glint_line = pow(max(0.0, 1.0 - abs(line_position)), shine_sharpness);
    
    // 5. Isolate metallic highlights
    float metal_highlight_mask = smoothstep(brightness_threshold, brightness_threshold + 0.1, brightness);
    vec3 final_shine = vec3(glint_line * shine_intensity * metal_highlight_mask);
    
    COLOR = vec4(tex_color.rgb + final_shine, tex_color.a);
}
