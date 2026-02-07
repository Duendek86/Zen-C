#version 330

in vec2 fragTexCoord;
in vec4 fragColor;
in vec3 fragNormal;

out vec4 finalColor;

uniform sampler2D texture0;
uniform vec4 colDiffuse;

// Dynamic Lighting Uniforms
uniform vec3 uLightDir;
uniform vec3 uLightCol;
uniform vec3 uAmbient;

void main()
{
    vec4 texelColor = texture(texture0, fragTexCoord);
    if (texelColor.a < 0.5) discard;
    
    // Normalize input dynamic direction
    vec3 lightDir = normalize(uLightDir);
    
    float NdotL = max(dot(fragNormal, lightDir), 0.0);
    vec3 diffuse = uLightCol * NdotL;
    
    vec3 lighting = uAmbient + diffuse;
    
    // Fake AO / Edge darkening to distinguish blocks
    // Softer Fake AO (Vignette style) to add depth without hard borders
    vec2 uv_center = fragTexCoord * 2.0 - 1.0;
    float dist_sq = dot(uv_center, uv_center); // 0 at center, 2 at corners
    float ao = 1.0 - (dist_sq * 0.1); // Max darken at corners is ~0.2
    
    lighting *= clamp(ao, 0.7, 1.0);
    
    // Dual Channel Lighting
    // fragColor.r = Block Light (0.0 - 1.0)
    // fragColor.g = Sky Light (0.0 - 1.0)
    
    float blockLight = fragColor.r;
    float skyLight = fragColor.g;
    
    // Sky Light is affected by Day/Night cycle (uLightCol) AND Directional Shading (NdotL)
    // Actually, Sky Light (from propagation) represents ambient sky visibility.
    // Direct Sun/Moon light should only affect surfaces facing it (NdotL) if they have Sky Light access.
    // But our propagation creates a scalar "light level".
    // Let's simplified model:
    // Sky Contribution = SkyLevel * SunColor * (Ambient + DiffuseShading)
    
    vec3 skyColor = uLightCol * (uAmbient + diffuse);
    vec3 skyContribution = skyColor * skyLight;
    
    // Block Light is warm and independent of sun
    vec3 torchColor = vec3(1.0, 0.8, 0.4); // Warm Torch
    vec3 blockContribution = torchColor * blockLight; // Linear falloff
    
    // Combine: Max or Add? 
    // Add is more physically correct for independent sources.
    vec3 combinedLight = skyContribution + blockContribution;
    
    // Add fake AO
    combinedLight *= clamp(ao, 0.7, 1.0);
    
    // Ensure minimum visibility
    combinedLight = max(combinedLight, vec3(0.01));
    
    finalColor = texelColor * colDiffuse * vec4(combinedLight, fragColor.a);
    
    // Simple distance fog - Reduced density for greater view distance
    float dist = gl_FragCoord.z / gl_FragCoord.w;
    float fogFactor = 1.0 / exp(dist * 0.015);
    fogFactor = clamp(fogFactor, 0.0, 1.0);
    finalColor = mix(vec4(0.53, 0.80, 0.92, 1.0), finalColor, fogFactor); // Sky color mix
}
