using Serilog;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
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
        private static RenderWindow win = new RenderWindow(new VideoMode(990, 470), "Chip 8 Emulator");

        // 4KB (4096 bytes) of memory
        private byte[] memory = new byte[4096];
        // 16 8-bit registers

        private int[] v = new int[16];
        private int i = 0;
        private int delayTimer = 0;
        private int soundTimer = 0;
        private int pc = 0x200;
        private Stack<int> stack = new Stack<int>();
        private bool paused = false;
        private int speed = 10;

        private static int cols = 64;
        private static int rows = 32;

        private int[] display = new int[cols * rows];
        private int scale = 15;

        private int onNextPress;

        public CPU()
        {
            win.SetFramerateLimit(60);
            win.SetVerticalSyncEnabled(true);
            win.SetKeyRepeatEnabled(false);

            win.Closed += Win_Closed;
            win.KeyPressed += new EventHandler<KeyEventArgs>(KeyPressed);
            win.KeyReleased += new EventHandler<KeyEventArgs>(KeyReleased);
        }


        private Dictionary<Keyboard.Key, int> keys = new Dictionary<Keyboard.Key, int>
        {
            { Keyboard.Key.Num1, 0x1 },
            { Keyboard.Key.Num2, 0x2 },
            { Keyboard.Key.Num3, 0x3 },
            { Keyboard.Key.Num4, 0xc },
            { Keyboard.Key.Q, 0x4 },
            { Keyboard.Key.W, 0x5 },
            { Keyboard.Key.E, 0x6 },
            { Keyboard.Key.R, 0xD },
            { Keyboard.Key.A, 0x7 },
            { Keyboard.Key.S, 0x8 },
            { Keyboard.Key.D, 0x9 },
            { Keyboard.Key.F, 0xE },
            { Keyboard.Key.Z, 0xA },
            { Keyboard.Key.X, 0x0 },
            { Keyboard.Key.C, 0xB },
            { Keyboard.Key.V, 0xF },
        };

        private Dictionary<int, bool> keyIsActive = new Dictionary<int, bool>
        {
            { 0x1, false },
            { 0x2, false },
            { 0x3, false },
            { 0xc, false },
            { 0x4, false },
            { 0x5, false },
            { 0x6, false },
            { 0xD, false },
            { 0x7, false },
            { 0x8, false },
            { 0x9, false },
            { 0xE, false },
            { 0xA, false },
            { 0x0, false },
            { 0xB, false },
            { 0xF, false },
        };

        private void Render()
        {
            for (var i = 0; i < cols * rows; i++)
            {
                var x = (i % cols) * scale;
                var y = (i / cols) * scale;

                if (Convert.ToBoolean(display[i]))
                {

                    RectangleShape pixel = new RectangleShape
                    {
                        Size = new Vector2f(1 * scale, 1 * scale),
                        FillColor = Color.White,
                        Position = new Vector2f(x, y)
                    };

                    win.Draw(pixel);
                }
            }
        }

        private bool IsKeyPressed(int keyCode)
        {
            return keyIsActive.FirstOrDefault(x => x.Key == keyCode).Value;
        }

        private void KeyPressed(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyPressed(e.Code))
            {
                if (keys.ContainsKey(e.Code))
                {
                    keyIsActive[keys[e.Code]] = true;
                }
            }

            if (keys.ContainsKey(e.Code))
            {
                if (keyIsActive[keys[e.Code]])
                {
                    onNextPress = keys.FirstOrDefault(x => x.Key == e.Code).Value;
                }
            }
        }

        private void KeyReleased(object sender, KeyEventArgs e)
        {
            if (keys.ContainsKey(e.Code))
            {
                keyIsActive[keys[e.Code]] = false;
            }
        }

        private bool SetPixel(int x, int y)
        {
            if (x > cols)
            {
                x -= cols;
            }
            else if (x < 0)
            {
                x += cols;
            }

            if (y > rows)
            {
                y -= rows;
            }
            else if (y < 0)
            {
                y += rows;
            }

            int pixelLoc = x + (y * cols);

            Array.Resize(ref display, cols * rows + pixelLoc * 2);

            display[pixelLoc] ^= 1;

            return !Convert.ToBoolean(display[pixelLoc]);
        }

        private void Clear()
        {
            display = new int[cols * rows];
            Log.Logger.Information("Screen cleared");
        }

        private static void Win_Closed(object sender, EventArgs e)
        {
            win.Close();
            Log.Logger.Information("Emulator closed");
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

            Log.Logger.Information("Loaded sprites into memory.");
        }

        public void LoadProgramIntoMemory(string path)
        {
            byte[] program = File.ReadAllBytes(path);

            for (int i = 0; i < program.Length; i++)
            {
                memory[0x200 + i] = program[i];
            }

            Log.Logger.Information("Program {path} loaded into memory.", path);
        }

        public void Cycle()
        {
            while (win.IsOpen)
            {
                for (int i = 0; i < speed; i++)
                {
                    if (!paused)
                    {
                        int opcode = memory[pc] << 8 | memory[pc + 1];
                        ExecuteInstruction(opcode);
                    }
                }

                if (!paused)
                {
                    UpdateTimers();
                }

                win.DispatchEvents();

                win.Clear(Color.Black);

                Render();

                PlaySound();

                win.Display();
            }
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
                Console.Beep(800, 10);
            }
            //else
            //{
            //    speaker.Stop();
            //}
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
                            Clear();
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
                            v[x] = v[y];
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
                            int sum = v[x] += v[y];

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
                            v[0xF] = v[x] & 0x1;
                            v[x] >>= 1;
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
                    i = opcode & 0xFFF;
                    break;
                case 0xB000:
                    pc = (opcode & 0xFFF) + v[0];
                    break;
                case 0xC000:
                    Random rand = new();
                    int randomGenerate = rand.Next(1, 255);

                    int random = randomGenerate * 0xFF;

                    v[x] = random & (opcode & 0xFF);
                    break;
                case 0xD000:
                    int width = 8;
                    int height = opcode & 0xF;

                    v[0xF] = 0;

                    for (int row = 0; row < height; row++)
                    {
                        var sprite = memory[i + row];

                        for (int col = 0; col < width; col++)
                        {
                            if ((sprite & 0x80) > 0)
                            {
                                if (SetPixel(v[x] + col, v[y] + row))
                                {
                                    v[0xF] = 1;
                                }
                            }

                            sprite <<= 1;
                        }
                    }
                    break;
                case 0xE000:
                    switch (opcode & 0xFF)
                    {
                        case 0x9E:
                            if (IsKeyPressed(v[x]))
                            {
                                pc += 2;
                            }
                            break;
                        case 0xA1:
                            if (!IsKeyPressed(v[x]))
                            {
                                pc += 2;
                            }
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

                            v[x] = onNextPress;
                            paused = false;
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
                            for (int registerIndex = 0; registerIndex <= x; registerIndex++)
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
                    Log.Logger.Error("Unknown opcode: {opcode}", opcode);
                    throw new Exception($"Unknown opcode{opcode}");
            }
        }
    }
}
