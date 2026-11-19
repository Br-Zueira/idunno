#version 330 core

in vec2 fragTexCoord;
in vec4 fragColor;

uniform vec3 u_cameraPos;
uniform vec3 u_cameraTarget;

uniform vec2 u_resolution;
uniform float u_fov;

out vec4 finalColor;

const int MAX_STEPS = 64;
const float MAX_DIST = 256.0;
const float SURF_DIST = 0.001;

const vec3 SPHERE_POS = vec3(10.0f, 0.0f, 10.0f);
const float RADIUS = 2.0;

float sdfSphere(float radius, vec3 spherePos, vec3 dotPos) {
    float dist = distance(spherePos, dotPos);
    return dist - radius;
}

void main() {
    float traveledDistance = 0.0f;
    vec3 dotPos = u_cameraPos;
    vec3 forward = normalize((u_cameraTarget - u_cameraPos));

    vec2 uv = (fragTexCoord * 2.0) - 1.0;
    uv.x *= u_resolution.x / u_resolution.y;

    float fovScale = tan(u_fov / 2);
    uv *= fovScale;

    vec3 worldUp = vec3(0.0, 1.0, 0.0);
    vec3 right = normalize(cross(forward, worldUp));
    vec3 up = cross(right, forward);
    vec3 rayDir = normalize(forward + (right * uv.x) + (up * uv.y));

    for (int steps = 0; steps < MAX_STEPS; steps++) {
        float dist = sdfSphere(RADIUS, SPHERE_POS, dotPos);
        
        if (dist < SURF_DIST) {
            finalColor = vec4(1.0f, 0.0f, 0.0f, 1.0f);
            return;
        } else {
            dotPos += rayDir * dist;
            traveledDistance += dist;
        }

        if (traveledDistance > MAX_DIST) {
            discard;
        } 
    }
    discard;
}