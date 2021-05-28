using SFML.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8Emulator
{
    public class Speaker : ISpeaker
    {
        private static SoundBuffer buffer = new SoundBuffer(@"D:\\kineyes.wav");
        private Sound sound = new Sound(buffer);

        public void Play(float gain, float pitch)
        {
            sound.Pitch = pitch;
            sound.Volume = gain;

            sound.Play();
        }

        public void Stop()
        {
            sound.Stop();
        }
    }
}
