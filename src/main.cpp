#include <raylib.h>
#include <iostream>

int main() {
    // Allows better debugging
    SetTraceLogLevel(LOG_ALL);

    // Set the configuration flags
    SetConfigFlags(FLAG_WINDOW_RESIZABLE | FLAG_VSYNC_HINT);

    // Initiating screen
    float screenWidth = 800.0f;
    float screenHeight = 450.0f;
    InitWindow(screenWidth, screenHeight, "Wtf am I doing?");

    if (!IsWindowReady()) {
        std::cerr << "Window not initialized" << std::endl;
        return 1;
    }

    // Unbids ESC from the default "Close Window" command
    SetExitKey(KEY_NULL);

    // Loading the program shader
    Shader shader = LoadShader(0, "resources/shader.fs");

    // Initiates the camera object
    Camera3D camera   = { 0 };
    camera.position   = (Vector3){ 0.0f, 2.0f, 5.0f };
    camera.target     = (Vector3){ 0.0f, 0.0f, 0.0f };
    camera.up         = (Vector3){ 0.0f, 1.0f, 0.0f };
    camera.fovy       = 45.0f;
    camera.projection = CAMERA_PERSPECTIVE;

    // Locations for the unifomm variables
    int camPosLoc           = GetShaderLocation(shader, "u_cameraPos");
    int camTargetLoc        = GetShaderLocation(shader, "u_cameraTarget");
    int screenDimensionsLoc = GetShaderLocation(shader, "u_resolution");
    int fovLoc              = GetShaderLocation(shader, "u_fov");
    int lightPosLoc         = GetShaderLocation(shader, "u_lightPos");
    int spherePosLoc        = GetShaderLocation(shader, "u_spherePos");
    int radiusLoc        = GetShaderLocation(shader, "u_radius");

    Vector3 lightPos  = (Vector3){ 10.0f, 64.0f, 10.0f };
    Vector3 spherePos = (Vector3){ 10.0f, 10.0f, 10.0f };
    float radius = 8.0f;

    // Window loop
    while (!WindowShouldClose()) {
        // Allows focusing in and out of program
        if (IsKeyPressed(KEY_ENTER)) DisableCursor();
        if (IsKeyPressed(KEY_ESCAPE)) EnableCursor();

        // Updates camera
        UpdateCamera(&camera, CAMERA_FREE);

        // Updates screen dimension data
        screenWidth = GetScreenWidth();
        screenHeight = GetScreenHeight();

        // Updates shader values
        // Camera related
        SetShaderValue(shader, camPosLoc, &camera.position, SHADER_UNIFORM_VEC3);
        SetShaderValue(shader, camTargetLoc, &camera.target, SHADER_UNIFORM_VEC3);
        SetShaderValue(shader, fovLoc, &camera.fovy, SHADER_UNIFORM_FLOAT);

        // Screen width and height
        Vector2 screenDimensions = { (float)screenWidth, (float)screenHeight };
        SetShaderValue(shader, screenDimensionsLoc, &screenDimensions, SHADER_UNIFORM_VEC2);

        // Sphere and lighting
        SetShaderValue(shader, lightPosLoc, &lightPos, SHADER_UNIFORM_VEC3);
        SetShaderValue(shader, spherePosLoc, &spherePos, SHADER_UNIFORM_VEC3);
        SetShaderValue(shader, radiusLoc, &radius, SHADER_UNIFORM_FLOAT);

        // Drawing loop
        BeginDrawing();
            // Cleans the last frame
            ClearBackground(BLACK);

            // Shader that will apply for the whole screen
            BeginShaderMode(shader);
                DrawRectangle(0.0f, 0.0f, screenWidth, screenHeight, WHITE);
            EndShaderMode();

            DrawFPS(10.0f, 10.0f);
        EndDrawing();
    }

    // Exit safely
    UnloadShader(shader);
    CloseWindow();
    return 0;
}