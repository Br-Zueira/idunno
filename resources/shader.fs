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

float sdfCube(vec3 scale, vec4 cubeOrientation, vec3 cubePos, vec3 dotPos) { 
    // Transform dot coordinate into cube relative coordinates
    vec3 p = dotPos - cubePos;

    // Rotate point location instead of rotating the cube itself
    vec4 invRot = vec4(-cubeOrientation.xyz, cubeOrientation.w);
    p = p + 2.0 * cross(invRot.xyz, cross(invRot.xyz, p) + invRot.w * p);

    // The actual SDF formula, ask to Inigo Quilez how this works because I dunno
    vec3 qBox = abs(p) - scale;
    return length(max(qBox, 0.0)) + min(max(qBox.x, max(qBox.y, qBox.z)), 0.0);
}

float sdfPlane(vec3 dotPos) {
    return abs(dotPos.y) - 0.25;
}

vec4 eulerToQuarternion(vec3 angles) {
    // Cos and sin versions of half the angles
    vec3 c = cos(angles * 0.5);
    vec3 s = sin(angles * 0.5);

    // The quaternion itself, currently empty
    vec4 q;

    // Some crazy ass conversion math
    q.x = s.x * c.y * c.z - c.x * s.y * s.z;
    q.y = c.x * s.y * c.z + s.x * c.y * s.z;
    q.z = c.x * c.y * s.z - s.x * s.y * c.z;
    q.w = c.x * c.y * c.z + s.x * s.y * s.z;

    return q;
}

// Returns the distance to the nearest prop
vec4 mapScene(vec3 p) {
    // All scene props
    float sphereDist = sdfSphere(u_radius, u_spherePos, p);
    float cubeDist = sdfCube(vec3(2.0f, 4.0f, 6.0f), eulerToQuarternion(vec3(45.0f, 30.0f, 15.0f)), vec3(20.0f, 30.0f, 40.0f), p);

    // Cube with a rounded hole
    float cubeCarve = sdfCube(vec3(6.0f, 6.0f, 6.0f), vec4(0.0f, 0.0f, 0.0f, 1.0f), vec3(25.0f, 10.0f, 25.0f), p);
    float sphereHole = sdfSphere(6.0f, vec3(25.0f, 16.0f, 25.0f), p);
    float carvedProp = max(cubeCarve, -sphereHole);

    // Grid modelation process:
    float planeDist = sdfPlane(p);

    // Repeats cubes infinitely
    vec3 q = p;
    vec2 spacing = vec2(4.5, 4.5);
    q.x = p.x - spacing.x * round(p.x / spacing.x);
    q.z = p.z - spacing.y * round(p.z / spacing.y);
    float cubicHoles = sdfCube(
        vec3(2.0, 0.5, 2.0), // Size of each cube
        vec4(0.0, 0.0, 0.0, 1.0), // Orientation
        vec3(0.0, 0.0, 0.0),
        q
    );
    
    // Carves the cubes from the grid
    float grid = max(planeDist, -cubicHoles);

    // Edit HERE and above to add new props
    const int propQuantity = 4;
    vec4 distList[propQuantity] = vec4[propQuantity](
        vec4(1.0, 0.0, 0.0, sphereDist), 
        vec4(0.0, 1.0, 1.0, cubeDist), 
        vec4(1.0, 0.0, 1.0, carvedProp),
        vec4(1.0, 1.0, 1.0, grid)
        );

    // Chooses the nearest distance and returns it
    vec4 closestProp = vec4(0.0, 0.0, 0.0, MAX_DIST + 1);

    for (int i = 0; i < propQuantity; i++) {
        if (distList[i].w < closestProp.w) {
            closestProp = distList[i];
        }
    } 

    return closestProp;
}

// Automatically calculates the correct normal for any shape
vec3 calcNormal(vec3 p) {
    float d = mapScene(p).w;
    vec2 e = vec2(0.001, 0.0); // Small offset
    
    vec3 n = d - vec3(
        mapScene(p - e.xyy).w,
        mapScene(p - e.yxy).w,
        mapScene(p - e.yyx).w
    );
    
    return normalize(n);
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

    // Gets whether camera is inside a prop
    bool isInside = mapScene(u_cameraPos).w < 0.0f;
    
    // Actual raymarching
    for (int steps = 0; steps < MAX_STEPS; steps++) {
        // Gets the smallest distance from any prop in the world
        vec4 nearestProp = mapScene(dotPos);
        float dist = nearestProp.w;
        
        // If inside the sphere, march outward
        if (isInside) dist = -dist;
        
        // If hit the obiect, render it, else keeps marching
        if (dist < SURF_DIST) {
            // Lighting calc
            vec3 normal = calcNormal(dotPos);
            vec3 lightDir = normalize(u_lightPos - dotPos);
            float diff = max(dot(normal, lightDir), 0.1);
            vec3 color = nearestProp.xyz * diff;

            // Fog effect
            vec3 fogColor = vec3(0.0);
            float fogFactor = exp(-traveledDistance * 0.005);
            vec3 finalSceneColor = mix(fogColor, color, fogFactor);

            finalColor = vec4(finalSceneColor, 1.0);
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