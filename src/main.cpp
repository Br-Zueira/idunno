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

    RenderTexture2D target = LoadRenderTexture(screenWidth, screenHeight);
    Shader shader = LoadShader(0, "resources/shader.fs");


    while (!WindowShouldClose()) {
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