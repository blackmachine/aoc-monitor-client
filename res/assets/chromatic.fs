#version 330

in vec2 fragTexCoord;
in vec4 fragColor;

uniform sampler2D texture0;
uniform vec2 texSize;  // размер текстуры в пикселях
uniform float offset;  // смещение в пикселях (анимируется)

out vec4 finalColor;

void main() {
    // Смещение в пикселях -> в UV координатах
    float offsetX = offset / texSize.x;
    
    // Сэмплируем каналы с разным смещением
    float r = texture(texture0, fragTexCoord + vec2(-offsetX, 0.0)).r;
    float g = texture(texture0, fragTexCoord).g;
    float b = texture(texture0, fragTexCoord + vec2(offsetX, 0.0)).b;
    float a = texture(texture0, fragTexCoord).a;
    
    finalColor = vec4(r, g, b, a) * fragColor;
}
