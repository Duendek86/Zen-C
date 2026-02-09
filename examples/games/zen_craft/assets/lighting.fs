#version 330

in vec2 fragTexCoord;
in vec4 fragColor; // r = Block Light, g = Sky Light
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

    // --- 1. PROCESADO DE LUZ ---
    
    // Extraer niveles de luz pre-calculados (Voxel Light)
    float blockLightLevel = fragColor.r; // 0.0 a 1.0
    float skyLightLevel = fragColor.g;   // 0.0 a 1.0
    
    // Apply curve to make caves darker faster
    // 4-5 blocks in should be dark.
    skyLightLevel = pow(skyLightLevel, 4.0);

    // Dirección del sol normalizada
    vec3 lightDir = normalize(uLightDir);
    
    // --- 2. CÁLCULO DE SOMBRAS SUAVES (Half-Lambert / Min Brightness) ---
    // En lugar de ir de 0.0 a 1.0, hacemos que vaya de 0.7 a 1.0.
    // Así, las caras a la sombra tienen al menos 70% de luminosidad base.
    float NdotL = dot(fragNormal, lightDir);
    float sunShadowFactor = 0.7 + 0.3 * max(NdotL, 0.0); 
    
    // --- 3. LUZ DEL CIELO (SKY LIGHT) ---
    // La luz del cielo depende de:
    // a) Cuánto cielo ve el bloque (skyLightLevel)
    // b) El color actual del sol/luna (uLightCol)
    // c) La luz ambiental base (uAmbient)
    // d) La sombra direccional de la cara (sunShadowFactor)
    
    vec3 skyBaseColor = uLightCol * sunShadowFactor;
    // Añadimos un poco de ambient para evitar negro absoluto
    vec3 effectiveSkyLight = skyBaseColor + uAmbient * 0.2; 
    
    vec3 skyContribution = effectiveSkyLight * skyLightLevel;

    // --- 4. LUZ DE ANTORCHA (BLOCK LIGHT) ---
    // Color cálido para antorchas
    vec3 torchColor = vec3(1.0, 0.85, 0.6); 
    // Decaimiento más suave para mas rango
    float blockIntensity = pow(blockLightLevel, 1.2);
    vec3 blockContribution = torchColor * blockIntensity;

    // --- 5. COMBINACIÓN ---
    // Usamos suma para combinar ambas fuentes
    vec3 combinedLight = skyContribution + blockContribution;

    // Fake AO (Vignette) - DISABLED/REDUCED per user request (Grid look)
    // vec2 uv_center = fragTexCoord * 2.0 - 1.0;
    // float dist_sq = dot(uv_center, uv_center);
    // float ao = 1.0 - (dist_sq * 0.4); 
    // combinedLight *= clamp(ao, 0.4, 1.0);

    // Evitar negro absoluto (siempre hay un mínimo de luz visible)
    combinedLight = max(combinedLight, vec3(0.02));

    // --- 5.5. WATER TINT ---
    // Water blocks are flagged with fragColor.b = 1.0 (255)
    // Apply blue tint and transparency to water blocks
    vec3 texColor = texelColor.rgb;
    float alpha = texelColor.a;
    
    // Water detection using Blue channel flag
    if (fragColor.b > 0.9) {
        // This is water - use solid blue color (no texture) and make semi-transparent
        texColor = vec3(0.2, 0.5, 0.9); // Cyan-blue color
        alpha = 0.6; // Semi-transparent
    }

    // --- 6. APLICAR COLOR FINAL ---
    vec3 finalRGB = texColor * colDiffuse.rgb * combinedLight;
    
    // NO gamma correction - linear output for more natural look

    // --- 7. NIEBLA DINÁMICA ---
    // La niebla debe coincidir con el color del cielo actual (uLightCol + uAmbient)
    // Calculamos un color promedio del cielo basado en la luz actual
    vec3 fogColor = mix(uAmbient, uLightCol, 0.5);
    
    // Clamp para que la niebla no brille demasiado
    fogColor = clamp(fogColor, 0.0, 0.9);

    float dist = gl_FragCoord.z / gl_FragCoord.w;
    // Niebla exponencial más densa para ocultar el fin del mundo
    float fogDensity = 0.02; 
    float fogFactor = 1.0 / exp(dist * fogDensity);
    fogFactor = clamp(fogFactor, 0.0, 1.0);

    // Si es de noche (luz muy baja), oscurecemos la niebla un poco más
    float brightness = (fogColor.r + fogColor.g + fogColor.b) / 3.0;
    if (brightness < 0.1) fogColor *= 0.5;
    
    vec3 foggedRGB = mix(fogColor, finalRGB, fogFactor);
    finalColor = vec4(foggedRGB, alpha);
}