using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8Emulator
{
    public class CPU : ICPU
    {
        // 4KB (4096 bytes) of memory
        private byte[] memory = new byte[4096];
        // 16 8-bit registers
        //private byte[] v = new byte[16];
        private int[] v = new int[16];
        private int i = 0;
        private int delayTimer = 0;
        private int soundTimer = 0;
        private int pc = 0x200;
        private Stack<int> stack = new Stack<int>();
        private bool paused = false;
        private int speed = 10;

        private readonly ISpeaker speaker;
        private readonly IDisplay display;
        private readonly IKeyboardModule keyboard;

        public CPU(ISpeaker speaker, IDisplay display, IKeyboardModule keyboard)
        {
            this.speaker = speaker;
            this.display = display;
            this.keyboard = keyboard;
        }

        public void LoadSpritesToMemory()
        {
            byte[] sprites = new byte[] {
                0xF0, 0x90, 0x90, 0x90, 0xF0,
                0x20, 0x60, 0x20, 0x20, 0x70,
                0xF0, 0x10, 0xF0, 0x80, 0xF0,
                0xF0, 0x10, 0xF0, 0x10, 0xF0,
                0x90, 0x90, 0xF0, 0x10, 0x10,
                0xF0, 0x80, 0xF0, 0x10, 0xF0,
                0xF0, 0x80, 0xF0, 0x90, 0xF0,
                0xF0, 0x10, 0x20, 0x40, 0x40,
                0xF0, 0x90, 0xF0, 0x90, 0xF0,
                0xF0, 0x90, 0xF0, 0x10, 0xF0,
                0xF0, 0x90, 0xF0, 0x90, 0x90,
                0xE0, 0x90, 0xE0, 0x90, 0xE0,
                0xF0, 0x80, 0x80, 0x80, 0xF0,
                0xE0, 0x90, 0x90, 0x90, 0xE0,
                0xF0, 0x80, 0xF0, 0x80, 0xF0,
                0xF0, 0x80, 0xF0, 0x80, 0x80
            };

            for (int i = 0; i < sprites.Length; i++)
            {
                memory[i] = sprites[i];
            }
        }

        public void LoadProgramIntoMemory(string path)
        {
            byte[] program = File.ReadAllBytes(path);

            for (int i = 0; i < program.Length; i++)
            {
                memory[0x200 + i] = program[i];
            }
        }

        public void Cycle()
        {
            for (int i = 0; i < speed; i++)
            {
                if (!paused)
                {
                    var opcode = memory[pc] << 8 | memory[pc + 1];
                    ExecuteInstruction(opcode);
                }
            }

            if (!paused)
            {
                UpdateTimers();
            }

            PlaySound();
            display.Render();
        }

        private void UpdateTimers()
        {
            if (delayTimer > 0)
            {
                delayTimer -= 1;
            }

            if (soundTimer > 0)
            {
                soundTimer -= 1;
            }
        }

        private void PlaySound()
        {
            if (soundTimer > 0)
            {
                speaker.Play(1, 440);
            }
            else
            {
                speaker.Stop();
            }
        }

        private void ExecuteInstruction(int opcode)
        {
            pc += 2;
            int x = (opcode & 0x0F00) >> 8;
            var y = (opcode & 0x00F0) >> 4;

            switch (opcode & 0xF000)
            {
                case 0x0000:
                    switch (opcode)
                    {
                        case 0x00E0:
                            display.Clear();
                            break;
                        case 0x00EE:
                            pc = stack.Pop();
                            break;
                    }

                    break;
                case 0x1000:
                    pc = (opcode & 0xFFF);
                    break;
                case 0x2000:
                    stack.Push(pc);
                    pc = (opcode & 0xFFF);
                    break;
                case 0x3000:
                    if (v[x] == (opcode & 0xFF))
                    {
                        pc += 2;
                    }
                    break;
                case 0x4000:
                    if (v[x] != (opcode & 0xFF))
                    {
                        pc += 2;
                    }
                    break;
                case 0x5000:
                    if (v[x] == v[y])
                    {
                        pc += 2;
                    }
                    break;
                case 0x6000:
                    v[x] = (opcode & 0xFF);
                    break;
                case 0x7000:
                    v[x] += (opcode & 0xFF);
                    break;
                case 0x8000:
                    switch (opcode & 0xF)
                    {
                        case 0x0:
                            v[y] = v[x];
                            break;
                        case 0x1:
                            v[x] |= v[y];
                            break;
                        case 0x2:
                            v[x] &= v[y];
                            break;
                        case 0x3:
                            v[x] ^= v[y];
                            break;
                        case 0x4:
                            var sum = (v[x] += v[y]);

                            v[0xF] = 0;

                            if (sum > 0xFF)
                            {
                                v[0xF] = 1;
                            }

                            v[x] = sum;
                            break;
                        case 0x5:
                            v[0xF] = 0;

                            if (v[x] > v[y])
                            {
                                v[0xF] = 1;
                            }

                            v[x] -= v[y];
                            break;
                        case 0x6:
                            v[0xF] = (v[x] & 0x1);
                            break;
                        case 0x7:
                            v[0xF] = 0;

                            if (v[y] > v[x])
                            {
                                v[0xF] = 1;
                            }

                            v[x] = v[y] - v[x];
                            break;
                        case 0xE:
                            v[0xF] = (v[x] & 0x80);
                            v[x] <<= 1;
                            break;
                    }

                    break;

                case 0x9000:
                    if (v[x] != v[y])
                    {
                        pc += 2;
                    }
                    break;
                case 0xA000:
                    i = (opcode & 0xFFF);
                    break;
                case 0xB000:
                    pc = (opcode & 0xFFF) + v[0];
                    break;
                case 0xC000:
                    var rand = new Random();
                    //int randNumber = rand.Next(0, 255);
                    //double randToDouble = Decimal.ToDouble(randNumber);
                    double randomDouble = rand.NextDouble() * 255;

                    //!Could be wrong
                    var random = Math.Floor(randomDouble * 0xFF);

                    int toInt = Convert.ToInt32(random);

                    v[x] = toInt & (opcode & 0xFF);
                    break;
                case 0xD000:
                    int width = 8;
                    int height = (opcode & 0xF);

                    v[0xF] = 0;

                    for (int row = 0; row < height; row++)
                    {
                        var sprite = memory[i + row];

                        for (int col = 0; col < width; col++)
                        {
                            if ((sprite & 0x80) > 0)
                            {
                                display.SetPixel(v[x] + col, v[y] + row);
                                v[0xF] = 1;
                            }

                            sprite <<= 1;
                        }
                    }
                    break;
                case 0xE000:
                    switch (opcode & 0xFF)
                    {
                        case 0x9E:
                            //Could be wrong
                            keyboard.IsKeyPressed(v[x]);
                            pc += 2;
                            break;
                        case 0xA1:
                            //keyboardstuff
                            break;
                    }

                    break;
                case 0xF000:
                    switch (opcode & 0xFF)
                    {
                        case 0x07:
                            v[x] = delayTimer;
                            break;
                        case 0x0A:
                            paused = true;

                            //keyboardstuff
                            break;
                        case 0x15:
                            delayTimer = v[x];
                            break;
                        case 0x18:
                            soundTimer = v[x];
                            break;
                        case 0x1E:
                            i += v[x];
                            break;
                        case 0x29:
                            i = v[x] * 5;
                            break;
                        case 0x33:
                            memory[i] = Convert.ToByte(v[x] / 100);

                            memory[i + 1] = Convert.ToByte(v[x] % 100 / 10);

                            memory[i + 2] = Convert.ToByte(v[x] % 10);
                            break;
                        case 0x55:
                            for (byte registerIndex = 0; registerIndex <= x; registerIndex++)
                            {
                                memory[i + registerIndex] = Convert.ToByte(v[registerIndex]);
                            }
                            break;
                        case 0x65:
                            for (int registerIndex = 0; registerIndex <= x; registerIndex++)
                            {
                                v[registerIndex] = memory[i + registerIndex];
                            }
                            break;
                    }

                    break;

                default:
                    throw new Exception($"Unknown opcode{opcode}");
            }
        }
    }
}
