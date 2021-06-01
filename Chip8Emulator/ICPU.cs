namespace Chip8Emulator
{
    public interface ICPU
    {
        void Cycle();
        bool IsKeyPressed(int keyCode);
        void LoadProgramIntoMemory(string path);
        void LoadSpritesToMemory();
        void Render();
        bool SetPixel(int x, int y);
    }
}