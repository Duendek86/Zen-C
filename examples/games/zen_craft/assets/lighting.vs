#version 330

in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec3 vertexNormal;
in vec4 vertexColor;

out vec2 fragTexCoord;
out vec4 fragColor;
out vec3 fragNormal;
out vec3 fragPosition;

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
    
    // Calculate World Position (inclusive of displacement for water?)
    // Actually we displace 'pos' later. 
    // We should calculate fragPosition AFTER displacement if we want correct depth/pos?
    // But 'matModel' is identity for chunks usually (except translation).
    // Chunks are drawn at 0,0,0 with baked coordinates?
    // No, Main.zc draws at (gx, 0, gz).
    
    // We want the World Position of the vertex.
    // We will update fragPosition at the end.
    
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
    
    // Water Wave Effect
    // Blue channel > 0.9 means Water (set to 255 in chunk_mesh)
    if (vertexColor.b > 0.9) {
         // Calculate World Position for consistent waves across chunks
         vec3 worldPos = (matModel * vec4(pos, 1.0)).xyz;
         
         // Apply vertical wave
         // Amplitude: 0.05 blocks (subtle)
         // Frequency: time * 2.0
         // Spatial freq: worldPos.x + worldPos.z
         float wave = sin(time * 3.0 + worldPos.x * 1.5 + worldPos.z * 1.0) * 0.05;
         pos.y += wave;
    }
    
    fragPosition = vec3(matModel * vec4(pos, 1.0));
    gl_Position = mvp * vec4(pos, 1.0);
}

