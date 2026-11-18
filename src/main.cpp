#include <raylib.h>

int main() {
    SetTraceLogLevel(LOG_ALL);
    
    InitWindow(800, 450, "Wtf am I doing?");

    if (!IsWindowReady()) {
        return 1;
    }

    while (!WindowShouldClose()) {
        BeginDrawing();
            ClearBackground(BLACK);
        EndDrawing();
    }

    CloseWindow();
    return 0;
}