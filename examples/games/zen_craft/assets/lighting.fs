#version 330

in vec2 fragTexCoord;
in vec4 fragColor;
in vec3 fragNormal;

out vec4 finalColor;

uniform sampler2D texture0;
uniform vec4 colDiffuse;

// Hardcoded simple light - Sun-ish
vec3 lightDir = normalize(vec3(0.8, 1.0, 0.5));
vec3 lightColor = vec3(0.9, 0.9, 0.85); // Warm sunlight
vec3 ambient = vec3(0.5, 0.5, 0.55);   // Cool ambient, darker for better contrast

void main()
{
    vec4 texelColor = texture(texture0, fragTexCoord);
    if (texelColor.a < 0.5) discard;
    // DEBUG: Force visible
    // if (texelColor.a < 0.5) discard;
    // finalColor = vec4(1.0, 0.0, 0.0, 1.0); return; 

    
    float NdotL = max(dot(fragNormal, lightDir), 0.0);
    vec3 diffuse = lightColor * NdotL;
    
    vec3 lighting = ambient + diffuse;
    
    // Fake AO / Edge darkening to distinguish blocks
    // Softer Fake AO (Vignette style) to add depth without hard borders
    vec2 uv_center = fragTexCoord * 2.0 - 1.0;
    float dist_sq = dot(uv_center, uv_center); // 0 at center, 2 at corners
    float ao = 1.0 - (dist_sq * 0.1); // Max darken at corners is ~0.2
    
    lighting *= clamp(ao, 0.7, 1.0);
    
    // Apply vertex color (contains light level from 0-255)
    // fragColor.rgb contains the light intensity (0.0 to 1.0)
    // Add minimum ambient light to prevent complete darkness
    vec3 vertexLight = fragColor.rgb;
    vertexLight = max(vertexLight, vec3(0.15)); // Minimum 15% brightness in caves
    
    finalColor = texelColor * colDiffuse * vec4(lighting, 1.0) * vec4(vertexLight, fragColor.a);
    
    // Simple distance fog - Reduced density for greater view distance
    float dist = gl_FragCoord.z / gl_FragCoord.w;
    float fogFactor = 1.0 / exp(dist * 0.015);
    fogFactor = clamp(fogFactor, 0.0, 1.0);
    finalColor = mix(vec4(0.53, 0.80, 0.92, 1.0), finalColor, fogFactor); // Sky color mix
}
