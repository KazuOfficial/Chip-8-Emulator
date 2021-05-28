namespace Chip8Emulator
{
    public interface ISpeaker
    {
        void Play(float gain, float pitch);
        void Stop();
    }
}