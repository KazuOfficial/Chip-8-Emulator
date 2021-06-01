using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8Emulator
{
    public class Chip8 : IChip8
    {
        private readonly ICPU cpu;
        //private readonly int fps = 60;

        public Chip8(ICPU cpu)
        {
            this.cpu = cpu;
        }

        public void Init()
        {
            //int fpsInterval = 1000 / 60;
            //int then = DateTime.Now.Millisecond;

            cpu.LoadSpritesToMemory();
            cpu.LoadProgramIntoMemory(@"D:\\8chiproms\\PONG");

            Step();
        }

        //public void Step(int fpsInterval, DateTime then)
        private void Step()
        {
            //int now = DateTime.Now.Millisecond;
            //int elapsed = now - then;

            cpu.Cycle();
        }
    }
}
