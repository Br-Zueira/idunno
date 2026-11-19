#version 330 core

in vec2 fragTexCoord;
in vec4 fragColor;

out vec4 finalColor;

void main() {
    vec2 uv = fragTexCoord;
    finalColor = vec4(uv.x, uv.y, 0.5, 1.0);
}