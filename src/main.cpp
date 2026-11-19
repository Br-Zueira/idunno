#include <raylib.h>
#include <iostream>

int main() {
    SetTraceLogLevel(LOG_ALL);

    int screenWidth = 800;
    int screenHeight = 450;
    InitWindow(screenWidth, screenHeight, "Wtf am I doing?");

    if (!IsWindowReady()) {
        std::cerr << "Window not initialized" << std::endl;
        return 1;
    }

    SetExitKey(KEY_NULL);

    RenderTexture2D target = LoadRenderTexture(screenWidth, screenHeight);
    Shader shader = LoadShader(0, "resources/shader.fs");

    Camera3D camera   = { 0 };
    camera.position   = (Vector3){ 0.0f, 2.0f, 5.0f };
    camera.target     = (Vector3){ 0.0f, 0.0f, 0.0f };
    camera.up         = (Vector3){ 0.0f, 1.0f, 0.0f };
    camera.fovy       = 45.0f;
    camera.projection = CAMERA_PERSPECTIVE;

    int camPosLoc = GetShaderLocation(shader, "u_cameraPos");
    int camTargetLoc = GetShaderLocation(shader, "u_cameraTarget");

    int screenDimensionsLoc = GetShaderLocation(shader, "u_resolution");
    int fovLoc = GetShaderLocation(shader, "u_fov");

    while (!WindowShouldClose()) {
        if (IsKeyPressed(KEY_ENTER)) {
            DisableCursor();
        }
        if (IsKeyPressed(KEY_ESCAPE)) {
            EnableCursor();
        }

        UpdateCamera(&camera, CAMERA_FREE);

        SetShaderValue(shader, camPosLoc, &camera.position, SHADER_UNIFORM_VEC3);
        SetShaderValue(shader, camTargetLoc, &camera.target, SHADER_UNIFORM_VEC3);

        Vector2 screenDimensions = { (float)screenWidth, (float)screenHeight };
        SetShaderValue(shader, screenDimensionsLoc, &screenDimensions, SHADER_UNIFORM_VEC2);

        SetShaderValue(shader, fovLoc, &camera.fovy, SHADER_UNIFORM_FLOAT);

        BeginDrawing();
            ClearBackground(BLACK);

            BeginShaderMode(shader);
                DrawTextureRec(
                    target.texture,
                    (Rectangle){ 0.0f, 0.0f, (float)target.texture.width, -(float)target.texture.height },
                    (Vector2){ 0.0f, 0.0f },
                    WHITE
                );
            EndShaderMode();
        EndDrawing();
    }

    UnloadRenderTexture(target);
    UnloadShader(shader);
    CloseWindow();
    return 0;
}