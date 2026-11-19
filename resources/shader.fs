#version 330 core

in vec2 fragTexCoord;
in vec4 fragColor;

uniform vec3 u_cameraPos;
uniform vec3 u_cameraTarget;
uniform float u_fov;
uniform vec2 u_resolution;

out vec4 finalColor;

const int MAX_STEPS = 64;
const float MAX_DIST = 256.0;
const float SURF_DIST = 0.001;

const vec3 SPHERE_POS = vec3(10.0f, 0.0f, 10.0f);
const float RADIUS = 2.0;

// Formula to know whether the point is inside or outside the object
float sdfSphere(float radius, vec3 spherePos, vec3 dotPos) {
    float dist = distance(spherePos, dotPos);
    return dist - radius;
}

void main() {
    // Where the point is and to where it must go
    vec3 dotPos = u_cameraPos;
    vec3 forward = normalize((u_cameraTarget - u_cameraPos));

    // Adjust to resolution
    vec2 uv = (fragTexCoord * 2.0) - 1.0;
    uv.x *= u_resolution.x / u_resolution.y;

    // Apply camera FOV
    float fovScale = tan(radians(u_fov) * 0.5);
    uv *= fovScale;

    // Orientates the ray/camera/idunno correctly
    vec3 worldUp = vec3(0.0, 1.0, 0.0);
    vec3 right = normalize(cross(forward, worldUp));
    vec3 up = cross(right, forward);

    // Actual direction the ray will go
    vec3 rayDir = normalize(forward + (right * uv.x) + (up * uv.y));
    
    // Used to know whether the ray should be discarded
    float traveledDistance = 0.0f;

    // Actual raymarching
    for (int steps = 0; steps < MAX_STEPS; steps++) {
        // Distance from sphere (currently the only prop in the world)
        float dist = sdfSphere(RADIUS, SPHERE_POS, dotPos);
        
        // If hit the obiect, render it, else keeps marching
        if (dist < SURF_DIST) {
            finalColor = vec4(1.0f, 0.0f, 0.0f, 1.0f);
            return;
        } else {
            dotPos += rayDir * dist;
            traveledDistance += dist;
        }

        // If the ray travelled too much without hitting anything, discard it
        if (traveledDistance > MAX_DIST) {
            discard;
        } 
    }
    discard;
}