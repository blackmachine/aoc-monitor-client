#version 330

in vec2 fragTexCoord;
in vec4 fragColor;

uniform sampler2D texture0;
uniform float time;
uniform vec2 resolution;

out vec4 finalColor;

// Настройки сканлайнов
float scanlinePeriod = 2.0; 
float opacityScanline = 0.3;
float opacityNoise = 0.3;

// 1. Увеличил силу мерцания (теперь это 15% изменения прозрачности вместо 1%)
float flickering = 0.05; 

// 2. Сила размытия (в пикселях). Можно поставить 2.0 или 3.0 для сильного мыла.
float blurSize = 1.0; 

const float PI = 3.14159265359;

float random(vec2 st) {
    return fract(sin(dot(st.xy, vec2(12.9898,78.233))) * 43758.5453123);
}

void main()
{
    vec2 uv = fragTexCoord;
    
    // Вычисляем размер одного пикселя в UV-координатах (от 0.0 до 1.0)
    vec2 texelSize = blurSize / resolution;
    
    // --- ПРОСТОЙ БЛЮР (5-tap) ---
    // Берем цвет в центре и чуть сдвигаемся по осям X и Y
    vec4 texColor = texture(texture0, uv);
    // texColor += 0.5*texture(texture0, uv + vec2(texelSize.x, 0.0));
    // texColor += 0.5*texture(texture0, uv + vec2(-texelSize.x, 0.0));
    // texColor += 0.5*texture(texture0, uv + vec2(0.0, texelSize.y));
    // texColor += 0.5*texture(texture0, uv + vec2(0.0, -texelSize.y));
    
    // // Делим на 5, так как мы сложили 5 текстурных выборок
    // texColor /= 3.0; 
    
    // --- СКАНЛАЙНЫ И ЭФФЕКТЫ ---
    float pixelY = uv.y * resolution.y;
    float scanlineValue = sin(pixelY * (2.0 * PI / scanlinePeriod));

    float currentAlpha = texColor.a;

    currentAlpha += currentAlpha * scanlineValue * opacityScanline;
    currentAlpha += currentAlpha * random(uv * time) * opacityNoise;
    
    // Изменил частоту мерцания на 15.0, чтобы пульсация была ритмичной и заметной
    currentAlpha += currentAlpha * sin(time * 60.0) * flickering;

    currentAlpha = clamp(currentAlpha, 0.0, 1.0);

    finalColor = vec4(texColor.rgb, currentAlpha) * fragColor;
}