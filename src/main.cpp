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

    // Creating the render texture 2D that will render the program and be upscaled, thus improving performance
    const float textureScaleFactor = 0.25;
    RenderTexture2D target = LoadRenderTexture(screenWidth * textureScaleFactor, screenHeight * textureScaleFactor);
    SetTextureFilter(target.texture, TEXTURE_FILTER_BILINEAR);

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
    int radiusLoc           = GetShaderLocation(shader, "u_radius");

    Vector3 lightPos  = (Vector3){ 10.0f, 64.0f, 10.0f };
    Vector3 spherePos = (Vector3){ 10.0f, 10.0f, 10.0f };
    float radius = 8.0f;

    // Window loop
    while (!WindowShouldClose()) {
        // Updates screen dimension data
        if (IsWindowResized) {
            screenWidth = GetScreenWidth();
            screenHeight = GetScreenHeight();

            UnloadRenderTexture(target);
            target = LoadRenderTexture(screenWidth * textureScaleFactor, screenHeight * textureScaleFactor);
            SetTextureFilter(target.texture, TEXTURE_FILTER_BILINEAR);
        }

        // Allows focusing in and out of program
        if (IsKeyPressed(KEY_ENTER)) DisableCursor();
        if (IsKeyPressed(KEY_ESCAPE)) EnableCursor();

        // Updates camera
        UpdateCamera(&camera, CAMERA_FREE);

        // Updates shader values
        // Camera related
        SetShaderValue(shader, camPosLoc, &camera.position, SHADER_UNIFORM_VEC3);
        SetShaderValue(shader, camTargetLoc, &camera.target, SHADER_UNIFORM_VEC3);
        SetShaderValue(shader, fovLoc, &camera.fovy, SHADER_UNIFORM_FLOAT);

        // Screen width and height
        Vector2 renderResolution = { (float)target.texture.width, (float)target.texture.height };
        SetShaderValue(shader, screenDimensionsLoc, &renderResolution, SHADER_UNIFORM_VEC2);

        // Sphere and lighting
        SetShaderValue(shader, lightPosLoc, &lightPos, SHADER_UNIFORM_VEC3);
        SetShaderValue(shader, spherePosLoc, &spherePos, SHADER_UNIFORM_VEC3);
        SetShaderValue(shader, radiusLoc, &radius, SHADER_UNIFORM_FLOAT);

        // Texture loop
        BeginTextureMode(target);
            // Cleans the last frame
            ClearBackground(BLACK);

            // Shader that will apply for the whole screen
            BeginShaderMode(shader);
                DrawRectangle(0, 0, target.texture.width, target.texture.height, WHITE);
            EndShaderMode();
        EndTextureMode();

        // Drawing loop
        BeginDrawing();
            // Cleans the last frame
            ClearBackground(BLACK);

            // Draws the processed frane
            DrawTexturePro(
                target.texture,
                (Rectangle){ 0, 0, (float)target.texture.width, (float)-target.texture.height },
                (Rectangle){ 0, 0, screenWidth, screenHeight },
                (Vector2){ 0, 0 },
                0.0f,
                WHITE
            );

            // Draws the FPS on top-left
            DrawFPS(10.0f, 10.0f);
        EndDrawing();
    }

    // Exit safely
    UnloadRenderTexture(target);
    UnloadShader(shader);
    CloseWindow();
    return 0;
}