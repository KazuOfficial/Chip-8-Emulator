namespace Chip8Emulator
{
    public interface IDisplay
    {
        void Clear();
        void Render();
        bool SetPixel(int x, int y);
    }
}