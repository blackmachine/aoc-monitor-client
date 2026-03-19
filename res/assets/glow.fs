#version 330

in vec2 fragTexCoord;
in vec4 fragColor;

uniform sampler2D texture0;
uniform vec2 resolution;

out vec4 finalColor;

// --- НАСТРОЙКИ КАЧЕСТВА ---
const int SAMPLES = 128;
const float RADIUS = 8.0;           // Можно смело крутить до 10-12
const float GOLDEN_ANGLE = 2.39996323;

// --- НАСТРОЙКИ СВЕТА И ЭКСПОЗИЦИИ ---
const float bloomIntensity = 2.0;   // Сила свечения
const float threshold = 0.55;       // Снизил порог, чтобы даже тусклые цвета давали ореол
const float gamma = 2;

// --- УПРАВЛЕНИЕ "ПЛЕНКОЙ" (Path to White) ---
const float exposure = 1.2;         
const float whitePoint = 2.0;       // Яркость, при которой цвет выжигается в чисто белый

float luminance(vec3 color) {
    return dot(color, vec3(0.2126, 0.7152, 0.0722));
}

void main()
{
    vec2 texelSize = 1.0 / resolution;
    vec4 source = texture(texture0, fragTexCoord);

    // 1. Исходник в линейное пространство
    vec3 sourceLin = pow(source.rgb, vec3(gamma));

    vec3 sumRgb = vec3(0.0);
    float totalWeight = 0.0;

    // 2. Спираль Фибоначчи (Vogel's Spiral)
    for (int i = 0; i < SAMPLES; i++)
    {
        float r = sqrt(float(i) + 0.5) / sqrt(float(SAMPLES));
        float theta = float(i) * GOLDEN_ANGLE;
        vec2 offset = vec2(cos(theta), sin(theta)) * (r * RADIUS);

        vec4 texColor = texture(texture0, fragTexCoord + offset * texelSize);
        
        // Линейный цвет выборки с учетом прозрачности
        vec3 linColor = pow(texColor.rgb, vec3(gamma)) * texColor.a;

        // Мягкий порог яркости
        float luma = luminance(linColor);
        float contrib = smoothstep(threshold, threshold + 0.1, luma);
        
        // Гауссово затухание (было 4.0, стало 2.0 — теперь ореол "шире" и мягче)
        float weight = exp(-r * r * 2.0); 

        sumRgb += linColor * contrib * weight;
        totalWeight += weight;
    }

    // 3. Собираем линейный свет
    vec3 bloomLin = (sumRgb / totalWeight) * bloomIntensity;
    
    // Складываем с оригиналом и применяем экспозицию
    vec3 resultLin = (sourceLin + bloomLin) * exposure;

    // --- 4. PATH TO WHITE (Стиль AgX) ---
    // Если линейная яркость пикселя выше 1.0, он начинает терять насыщенность
    // и стремительно выцветать в чистый белый к моменту достижения whitePoint.
    float currentLuma = luminance(resultLin);
    float desat = smoothstep(1.0, whitePoint, currentLuma);
    resultLin = mix(resultLin, vec3(currentLuma), desat);

    // Защита от значений > 1.0 перед выводом на экран
    resultLin = clamp(resultLin, 0.0, 1.0);

    // --- 5. ВОЗВРАТ В sRGB ---
    // Обычная гамма вместо ACES. Именно она вытягивает слабый ореол из темноты!
    vec3 resultSrgb = pow(resultLin, vec3(1.0 / gamma));

    // Умная прозрачность для корректного наложения на фон
    float glowOpacity = luminance(pow(bloomLin, vec3(1.0 / gamma)));
    float finalAlpha = clamp(source.a + glowOpacity, 0.0, 1.0);

    finalColor = vec4(resultSrgb, finalAlpha) * fragColor;
}