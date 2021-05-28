﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8Emulator
{
    public class Chip8 : IChip8
    {
        private readonly ICPU cpu;
        private readonly int fps = 60;

        public Chip8(ICPU cpu)
        {
            this.cpu = cpu;
        }

        public void Init()
        {
            int fpsInterval = 1000 / fps;
            DateTime then = DateTime.Now;

            cpu.LoadSpritesToMemory();
            cpu.LoadProgramIntoMemory(@"D:\\BLITZ");

            Step(fpsInterval, then);
        }

        private void Step(int fpsInterval, DateTime then)
        {
            DateTime now = DateTime.Now;
            TimeSpan elapsed = now - then;

            if (elapsed.TotalMilliseconds > fpsInterval)
            {
                cpu.Cycle();
            }
        }
    }
}
