#version 330

in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec3 vertexNormal;
in vec4 vertexColor;

out vec2 fragTexCoord;
out vec4 fragColor;
out vec3 fragNormal;

uniform mat4 mvp;
uniform mat4 matModel;
uniform mat4 matNormal;

uniform float time;

void main()
{
    fragTexCoord = vertexTexCoord;
    fragColor = vertexColor;
    fragNormal = normalize(vec3(matNormal * vec4(vertexNormal, 1.0)));
    
    vec3 pos = vertexPosition;
    
    // Wind Effect
    // Blue channel 100 (approx 0.39) -> Wind
    // Blue channel 255 (1.0) -> Water
    if (vertexColor.b > 0.3 && vertexColor.b < 0.5) { // 100/255 = 0.39
         // Apply swaying
         // Higher vertices sway more (for grass/plants)
         // But we don't have height info easily for blocks (leaves).
         // However, leaves are usually high up.
         // For grass_deco (crossed quads), texture V goes 1.0 (bottom) to 0.5 (top).
         // So inverted V can be used as mask?
         // vertexTexCoord.y: 1.0 -> 0.0 sway, 0.5 -> 1.0 sway?
         // Let's just sway everything slightly for now, or use UV y if possible.
         
         // Simple sway
         float sway = sin(time * 3.0 + pos.x * 0.5 + pos.z * 0.5) * 0.1;
         
         pos.x += sway;
         pos.z += sway * 0.5; // Slight Z movement too
    }
    
    gl_Position = mvp * vec4(pos, 1.0);
}
