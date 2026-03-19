#version 330

// Input vertex attributes (from vertex shader)
in vec2 fragTexCoord;
in vec4 fragColor;

// Input uniform values
uniform sampler2D texture0;      // Основная текстура (глиф)
uniform sampler2D texture1;      // Текстура маски (может быть та же или другая)
uniform vec4 maskSrcRect;        // x, y, width, height маски в пикселях текстуры маски
uniform vec2 maskTexSize;        // Размер текстуры маски в пикселях
uniform vec4 glyphSrcRect;       // x, y, width, height глифа в пикселях основной текстуры
uniform vec2 glyphTexSize;       // Размер основной текстуры в пикселях
uniform int useMask;             // 1 = использовать маску, 0 = нет
uniform vec4 bgColor;            // Цвет фона (RGBA, 0-1)

// Output fragment color
out vec4 finalColor;

void main() {
    // Получаем цвет основного глифа
    vec4 glyphColor = texture(texture0, fragTexCoord) * fragColor;
    
    if (useMask == 1) {
        // Вычисляем локальные UV координаты внутри глифа [0..1]
        vec2 glyphUV = (fragTexCoord * glyphTexSize - glyphSrcRect.xy) / glyphSrcRect.zw;
        
        // Преобразуем в UV координаты маски
        vec2 maskUV = (maskSrcRect.xy + glyphUV * maskSrcRect.zw) / maskTexSize;
        
        // Получаем альфу маски
        vec4 maskColor = texture(texture1, maskUV);
        float maskAlpha = maskColor.a;
        
        // Применяем маску к глифу
        vec4 maskedGlyph = vec4(glyphColor.rgb, glyphColor.a * maskAlpha);
        
        // Применяем маску к фону
        vec4 maskedBg = vec4(bgColor.rgb, bgColor.a * maskAlpha);
        
        // Смешиваем фон и глиф (глиф поверх фона)
        // standard alpha blending: result = src * srcAlpha + dst * (1 - srcAlpha)
        float outAlpha = maskedGlyph.a + maskedBg.a * (1.0 - maskedGlyph.a);
        vec3 outRgb;
        if (outAlpha > 0.0) {
            outRgb = (maskedGlyph.rgb * maskedGlyph.a + maskedBg.rgb * maskedBg.a * (1.0 - maskedGlyph.a)) / outAlpha;
        } else {
            outRgb = vec3(0.0);
        }
        
        finalColor = vec4(outRgb, outAlpha);
    } else {
        finalColor = glyphColor;
    }
}
