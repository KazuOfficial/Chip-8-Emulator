namespace Chip8Emulator
{
    public interface ICPU
    {
        void Cycle();
        void LoadProgramIntoMemory(string path);
        void LoadSpritesToMemory();
    }
}