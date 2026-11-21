#version 330 core

uniform vec3 u_cameraPos;
uniform vec3 u_cameraTarget;
uniform float u_fov;
uniform vec2 u_resolution;
uniform vec3 u_lightPos;
uniform vec3 u_spherePos;
uniform float u_radius;
uniform float u_time;

out vec4 finalColor;

const int MAX_STEPS = 64;
const float MAX_DIST = 300.0;
const float SURF_DIST = 0.001;

// Formula to know whether the point is inside or outside the object
float sdfSphere(float radius, vec3 spherePos, vec3 dotPos) {
    float dist = distance(spherePos, dotPos);
    return dist - radius;
}

float sdfCube(vec3 scale, vec4 cubeOrientation, vec3 cubePos, vec3 dotPos) { 
    // Transform dot coordinate into cube relative coordinates
    vec3 p = dotPos - cubePos;

    // Optimization (ignores rotation if the cube is axis aligned)
    if (cubeOrientation != vec4(0.0f)) {
        // Rotate point location instead of rotating the cube itself
        vec4 invRot = vec4(-cubeOrientation.xyz, cubeOrientation.w);
        p = p + 2.0 * cross(invRot.xyz, cross(invRot.xyz, p) + invRot.w * p);
    }

    // The actual SDF formula, ask to Inigo Quilez how this works because I dunno
    vec3 qBox = abs(p) - scale;
    return length(max(qBox, 0.0)) + min(max(qBox.x, max(qBox.y, qBox.z)), 0.0);
}

float sdfPlane(vec3 dotPos) {
    return abs(dotPos.y) - 0.25;
}

// Helper for a crisp 3D wireframe box
float sdBoxFrame3D(vec3 p, vec3 b, float e) {
    p = abs(p) - b;
    vec3 q = abs(p + e) - e;
    return min(min(
        length(max(vec3(p.x, q.y, q.z), 0.0)) + min(max(p.x, max(q.y, q.z)), 0.0),
        length(max(vec3(q.x, p.y, q.z), 0.0)) + min(max(q.x, max(p.y, q.z)), 0.0)),
        length(max(vec3(q.x, q.y, p.z), 0.0)) + min(max(q.x, max(q.y, p.z)), 0.0));
}

float sdfTesseract(vec4 scale, vec3 cubePos, float wFac, float wFac2, vec3 dotPos) {
    vec3 p = dotPos - cubePos;

    // 1. Define the 8 corners of a 3D cube, duplicated at W = +1 and W = -1 (16 vertices total)
    // We can fold X, Y, Z with abs() so we only need to check the positive octant!
    vec3 q = abs(p);
    
    // Angle setup for 4D rotations (XW and YW)
    float a1 = u_time * wFac;
    float c1 = cos(a1), s1 = sin(a1);
    float a2 = u_time * wFac2;
    float c2 = cos(a2), s2 = sin(a2);

    // Project the two 4D hyper-faces (w = +scale.w and w = -scale.w) into 3D space
    // For a classic tesseract projection, we need the inner & outer 3D cubes + connecting struts
    float wDist = scale.w * 2.5; // Camera distance in 4D (controls inner cube size)
    
    // Project 4D vertex (+scale.xyz, +scale.w) and (+scale.xyz, -scale.w)
    vec4 vPos = vec4(scale.xyz,  scale.w);
    vec4 vNeg = vec4(scale.xyz, -scale.w);

    // Rotate both in XW
    vPos.xw = mat2(c1, -s1, s1, c1) * vPos.xw;
    vNeg.xw = mat2(c1, -s1, s1, c1) * vNeg.xw;
    
    // Rotate both in YW
    vPos.yw = mat2(c2, -s2, s2, c2) * vPos.yw;
    vNeg.yw = mat2(c2, -s2, s2, c2) * vNeg.yw;

    // Perspective divide from 4D -> 3D
    vec3 p3Pos = vPos.xyz * (wDist / (wDist - vPos.w));
    vec3 p3Neg = vNeg.xyz * (wDist / (wDist - vNeg.w));

    float thickness = 0.25;

    // 1. Diagonal struts connecting the inner and outer projected cubes
    vec3 pa = q - p3Neg;
    vec3 ba = p3Pos - p3Neg;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    float strut = length(pa - ba * h) - thickness;

    // 2. Edges of the two projected cubes (in the folded octant)
    // Distance from q to the axis-aligned edges meeting at p3Pos and p3Neg
    vec3 dPos = abs(q - p3Pos);
    float edgesPos = min(min(
        length(vec2(dPos.y, dPos.z)) * step(q.x, abs(p3Pos.x)),
        length(vec2(dPos.x, dPos.z)) * step(q.y, abs(p3Pos.y))),
        length(vec2(dPos.x, dPos.y)) * step(q.z, abs(p3Pos.z)));
    // Use proper box frame for exact axis-aligned edges when not skewed:
    float box1 = sdBoxFrame3D(p, abs(p3Pos), thickness);
    float box2 = sdBoxFrame3D(p, abs(p3Neg), thickness);

    return min(min(box1, box2), strut);
}

vec4 eulerToQuarternion(vec3 angles) {
    vec3 rads = radians(angles);

    // Cos and sin versions of half the angles
    vec3 c = cos(rads * 0.5);
    vec3 s = sin(rads * 0.5);

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

    // Make the cube rotate in all axis
    float rotFac = u_time * 60.0f;
    float cubeDist = sdfCube(vec3(2.0f, 4.0f, 6.0f), eulerToQuarternion(vec3(rotFac + 45.0f, rotFac, rotFac + 15.0f)), vec3(20.0f, 30.0f, 40.0f), p);

    // Cube with a rounded hole
    float cubeCarve = sdfCube(vec3(6.0f, 6.0f, 6.0f), vec4(0.0f), vec3(25.0f, 10.0f, 25.0f), p);
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
        vec4(0.0f), // Orientation
        vec3(0.0, 0.0, 0.0),
        q
    );
    
    // Carves the cubes from the grid
    float grid = max(planeDist, -cubicHoles);

    // Tesseract where dot moves into w axis according to time
    float tesseract = sdfTesseract(vec4(8.0f), vec3(-20.0f, 15.0f, -5.0f), 0.5f, 0.75f, p);

    // Edit HERE and above to add new props
    const int propQuantity = 5;
    vec4 distList[propQuantity] = vec4[propQuantity](
        vec4(1.0, 0.0, 0.0, sphereDist), 
        vec4(0.0, 1.0, 1.0, cubeDist), 
        vec4(1.0, 0.0, 1.0, carvedProp),
        vec4(1.0, 1.0, 1.0, grid),
        vec4(0.0, 1.0, 0.0, tesseract)
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
    vec3 n = vec3(0.0);
    for (int i = 0; i < 4; i++) {
        // Generates 4 symmetrical tetrahedron offset vectors
        vec3 e = 0.5773 * (2.0 * vec3(((i + 3) >> 1) & 1, (i >> 1) & 1, i & 1) - 1.0);
        n += e * mapScene(p + e * 0.001).w;
    }
    
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
            float fogFactor = exp(-traveledDistance * 0.02);
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