#version 330

in vec2 fragTexCoord;
in vec4 fragColor; // r = Block Light, g = Sky Light
in vec3 fragNormal;

out vec4 finalColor;

uniform sampler2D texture0;

// Dynamic Lighting Uniforms
uniform vec3 uLightDir;
uniform vec3 uLightCol;
uniform vec3 uAmbient;
uniform vec3 viewPos;
uniform float time;
// removed colDiffuse to avoid uninitialized uniform issues

in vec3 fragPosition;

void main()
{
    vec4 texelColor = texture(texture0, fragTexCoord);
    if (texelColor.a < 0.5) discard;
    
    vec3 texColor = texelColor.rgb; // Initialize defaults
    float alpha = texelColor.a;

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

    // --- 5.5. WATER TINT & REFLECTION ---
    // Water blocks are flagged with fragColor.b > 0.9
    
    vec3 viewDir = normalize(viewPos - fragPosition);

    if (fragColor.b > 0.9) {
        // 1. DISTORTION (Fake Waves)
        // Perturb the normal slightly based on position and time
        // This makes specular and reflection dance
        vec3 distortedNormal = fragNormal;
        float waveSpeed = 2.0;
        float waveScale = 0.5;
        
        // Simple noise-like function using sin/cos
        float n_x = sin(fragPosition.x * 2.0 + time * waveSpeed) * 0.1;
        float n_z = cos(fragPosition.z * 2.0 + time * waveSpeed * 0.8) * 0.1;
        
        distortedNormal.x += n_x;
        distortedNormal.z += n_z;
        distortedNormal = normalize(distortedNormal);
        
        // Re-calculate NdotL with distorted normal for dynamic highlights
        float distNdotL = max(dot(distortedNormal, lightDir), 0.0);
        
        // 2. REFLECTION (Fresnel)
        // Everything reflects the sky for now.
        // Sky Color is roughly skyBaseColor (but brighter).
        // Let's use uLightCol/uAmbient blend as "Sky Env Map".
        vec3 skyReflectColor = mix(uAmbient, uLightCol, 0.5);
        if (skyReflectColor.g > 0.5) skyReflectColor = vec3(0.6, 0.8, 1.0); // Daylight blue
        else skyReflectColor = vec3(0.05, 0.05, 0.1); // Night dark
        
        // Fresnel Schlick approximation
        float fresnel = pow(1.0 - max(dot(viewDir, distortedNormal), 0.0), 3.0);
        fresnel = clamp(fresnel, 0.0, 1.0);
        
        // Base Water Color (Deep Blue)
        vec3 waterBase = vec3(0.1, 0.3, 0.8);
        
        // Mix Base and Sky based on Fresnel
        // High angle (looking down) -> See water/ground (fresnel low)
        // Low angle (looking grazing) -> See sky reflection (fresnel high)
        
        texColor = mix(waterBase, skyReflectColor, fresnel * 0.6);
        
        // Add Specular Highlight (Sun)
        vec3 reflectDir = reflect(-lightDir, distortedNormal);
        float spec = pow(max(dot(viewDir, reflectDir), 0.0), 32.0);
        texColor += uLightCol * spec * 0.5; // Sun glint
        
        alpha = 0.7; // Slightly more opaque to see color, but still transparent
    }

    // --- 6. APLICAR COLOR FINAL ---
    vec3 finalRGB = texColor * combinedLight;
    
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