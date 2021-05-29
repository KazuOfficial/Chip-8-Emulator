namespace Chip8Emulator
{
    public interface ICPU
    {
        void Clear();
        void Cycle();
        void LoadProgramIntoMemory(string path);
        void LoadSpritesToMemory();
        void SetPixel(int x, int y);
    }
}