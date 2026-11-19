#version 330 core

uniform vec3 u_cameraPos;
uniform vec3 u_cameraTarget;
uniform float u_fov;
uniform vec2 u_resolution;
uniform vec3 u_lightPos;
uniform vec3 u_spherePos;
uniform float u_radius;

out vec4 finalColor;

const int MAX_STEPS = 64;
const float MAX_DIST = 256.0;
const float SURF_DIST = 0.001;

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
    vec2 uv = (gl_FragCoord.xy * 2.0 - u_resolution) / u_resolution.y;

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

    // Gets whether camera is inside the sphere or not
    bool isInside = sdfSphere(u_radius, u_spherePos, u_cameraPos) < 0.0f;
    
    // Actual raymarching
    for (int steps = 0; steps < MAX_STEPS; steps++) {
        // Distance from sphere (currently the only prop in the world)
        float dist = sdfSphere(u_radius, u_spherePos, dotPos);
        
        // If inside the sphere, march outward
        if (isInside) dist = -dist;
        
        // If hit the obiect, render it, else keeps marching
        if (dist < SURF_DIST) {
            vec3 normal = normalize(dotPos - u_spherePos);

            vec3 lightDir = normalize(u_lightPos - dotPos);
            float diff = max(dot(normal, lightDir), 0.1);
            finalColor = vec4(diff, 0.0f, 0.0f, 1.0f);
            return;
        } else {
            dotPos += rayDir * abs(dist);
            traveledDistance += abs(dist);
        }

        // If the ray travelled too much without hitting anything, discard it
        if (traveledDistance > MAX_DIST) {
            discard;
        } 
    }
    discard;
}