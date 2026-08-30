shader_type canvas_item;

// Controls how fast the warble moves
uniform float speed : hint_range(0.1, 5.0) = 1.0;
// Controls how violent/large the distortion is
uniform float frequency : hint_range(1.0, 50.0) = 15.0;
// Controls the wave depth
uniform float amplitude : hint_range(0.001, 0.1) = 0.01;
// Unique offset to desync instances
uniform float offset = 0.0;

void fragment() {
    vec2 uv = UV;
    
    // Add the offset to the time variable to change the starting rhythm
    float wave = sin(uv.y * frequency - ((TIME + offset) * speed)) * amplitude;
    
    // Apply the horizontal distortion to the X coordinate
    uv.x += wave;
    
    // Read the texture using the distorted coordinates
    COLOR = texture(TEXTURE, uv);
}
