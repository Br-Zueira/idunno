#version 330 core

in vec2 fragTexCoord;
in vec4 fragColor;

uniform vec3 u_cameraPos;
uniform vec3 u_cameraTarget;

out vec4 finalColor;

void main() {
    finalColor = vec4(u_cameraPos.x, u_cameraTarget.y, 0.5, 1.0);
}