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
        private static RenderWindow win = new RenderWindow(new SFML.Window.VideoMode(64, 32), "Chip 8 Emulator");
        //private static int vectorListSize = 1;
        //private static Vector2f[] vectors = new Vector2f[vectorListSize];
        private static List<Vector2f> vectors = new List<Vector2f>();

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

        public CPU(ISpeaker speaker)
        {
            this.speaker = speaker;
            win.SetVerticalSyncEnabled(true);
            win.SetFramerateLimit(60);
            win.Closed += Win_Closed;
            win.SetKeyRepeatEnabled(false);
            win.KeyPressed += new EventHandler<KeyEventArgs>(KeyPressed);
            win.KeyReleased += new EventHandler<KeyEventArgs>(KeyReleased);
        }

        //Keyboard

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

        private int[] keysPressed;

        public bool IsKeyPressed(int keyCode)
        {
            return keyIsActive.FirstOrDefault(x => x.Key == keyCode).Value;
        }

        private void KeyPressed(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyPressed(e.Code))
            {
                keyIsActive[keys[e.Code]] = true;
                Log.Logger.Information("PRESSED KEY!!!!!!!!!!!!!!!: {pressed}", e.Code);
            }
        }

        private void KeyReleased(object sender, KeyEventArgs e)
        {
            keyIsActive[keys[e.Code]] = false;
            Log.Logger.Information("RELEASED!!!!!!!!!!!!!!!: {released}", e.Code);
        }

        //Graphics

        public void SetPixel(int x, int y)
        {
            int cols = 64;
            int rows = 32;

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

            //int pixelLoc = x + (y + cols);

            //RectangleShape pixel = new RectangleShape
            //{
            //    Size = new Vector2f(1, 1),
            //    FillColor = Color.White,
            //    Position = new Vector2f(x, y)
            //};

            //vectorListSize++;
            vectors.Add(new Vector2f(x, y));

            Log.Logger.Information("Pixel has been drawn. x: {x} y: {y}", x, y);
        }

        public void Clear()
        {
            win.Clear(Color.Black);
            Log.Logger.Information("Screen cleared");
        }

        private static void Win_Closed(object sender, EventArgs e)
        {
            win.Close();
            Log.Logger.Information("Window closed");
        }

        //CPU

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

            Log.Logger.Information("Loaded sprites to memory. Sprites: {sprites}, memory: {memory}", sprites, memory);
        }

        public void LoadProgramIntoMemory(string path)
        {
            byte[] program = File.ReadAllBytes(path);

            for (int i = 0; i < program.Length; i++)
            {
                memory[0x200 + i] = program[i];
            }

            Log.Logger.Information("Program loaded into memory. Program's path: {program}, program's bytes: {bytes}", path, program);
        }

        public void Cycle()
        {
            while (win.IsOpen)
            {
                Log.Logger.Information("Cycle started.");
                for (int i = 0; i < speed; i++)
                {
                    if (!paused)
                    {
                        var opcode = memory[pc] << 8 | memory[pc + 1];
                        ExecuteInstruction(opcode);
                        Log.Logger.Information("Emulator paused: {paused}, opcode: {opcode}", paused, opcode);
                    }
                }

                if (!paused)
                {
                    UpdateTimers();
                }

                PlaySound();
                win.DispatchEvents();
                win.Clear(Color.Black);
                foreach (Vector2f vector2F in vectors)
                {
                    win.Draw(new RectangleShape
                    {
                        Size = new Vector2f(1, 1),
                        FillColor = Color.White,
                        Position = vector2F
                    });
                }
                Log.Logger.Information("Vectors: {vectors}.", vectors);
                win.Display();
                Log.Logger.Information("Cycle ended.");
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

            Log.Logger.Information("Timers updated. DelayTimer: {delayTimer}, SoundTimer: {soundTimer}", delayTimer, soundTimer);
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

            Log.Logger.Information("Sound played");
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
                            win.Clear();
                            Log.Logger.Information("Instruction 0x00E0 called. Window cleared.");
                            break;
                        case 0x00EE:
                            pc = stack.Pop();
                            Log.Logger.Information("Instruction 0x00EE called. pc = stack.Pop(). PC: {pc}.", pc);
                            break;
                    }

                    break;
                case 0x1000:
                    pc = (opcode & 0xFFF);
                    Log.Logger.Information("Instruction 0x1000 called. pc = (opcode & 0xFFF). PC: {pc}.", pc);
                    break;
                case 0x2000:
                    stack.Push(pc);
                    pc = (opcode & 0xFFF);
                    Log.Logger.Information("Instruction 0x2000 called. stack.Push(pc); pc = (opcode & 0xFFF). PC: {pc}.", pc);
                    break;
                case 0x3000:
                    if (v[x] == (opcode & 0xFF))
                    {
                        pc += 2;
                        Log.Logger.Information("Instruction 0x3000 called. if (v[x] == (opcode & 0xFF)) THEN pc+= 2. PC: {pc}.", pc);
                    }
                    break;
                case 0x4000:
                    if (v[x] != (opcode & 0xFF))
                    {
                        pc += 2;
                        Log.Logger.Information("Instruction 0x4000 called. if (v[x] != (opcode & 0xFF)) THEN pc+= 2. PC: {pc}.", pc);
                    }
                    break;
                case 0x5000:
                    if (v[x] == v[y])
                    {
                        pc += 2;
                        Log.Logger.Information("Instruction 0x5000 called. if (v[x] == v[y]) THEN pc+= 2. PC: {pc}.", pc);
                    }
                    break;
                case 0x6000:
                    v[x] = (opcode & 0xFF);
                    Log.Logger.Information("Instruction 0x6000 called. v[x] = (opcode & 0xFF). VX: {vx}.", v[x]);
                    break;
                case 0x7000:
                    v[x] += (opcode & 0xFF);
                    Log.Logger.Information("Instruction 0x7000 called. v[x] += (opcode & 0xFF). VX: {vx}.", v[x]);
                    break;
                case 0x8000:
                    switch (opcode & 0xF)
                    {
                        case 0x0:
                            v[y] = v[x];
                            Log.Logger.Information("Instruction 0x8000 called. v[y] = v[x]. v[y]: {vy}", v[y]);
                            break;
                        case 0x1:
                            v[x] |= v[y];
                            Log.Logger.Information("Instruction 0x8001 called. v[x] |= v[y]. v[x]: {vx}", v[x]);
                            break;
                        case 0x2:
                            v[x] &= v[y];
                            Log.Logger.Information("Instruction 0x8002 called. v[x] &= v[y]. v[x]: {vx}", v[x]);
                            break;
                        case 0x3:
                            v[x] ^= v[y];
                            Log.Logger.Information("Instruction 0x8003 called. v[x] ^= v[y]. v[x]: {vx}", v[x]);
                            break;
                        case 0x4:
                            var sum = (v[x] += v[y]);

                            v[0xF] = 0;

                            if (sum > 0xFF)
                            {
                                v[0xF] = 1;
                            }

                            v[x] = sum;
                            Log.Logger.Information("Instruction 0x8004 called. ... v[x] = sum. sum: {sum}, v[x]: {vx}", sum, v[x]);
                            break;
                        case 0x5:
                            v[0xF] = 0;

                            if (v[x] > v[y])
                            {
                                v[0xF] = 1;
                            }

                            v[x] -= v[y];
                            Log.Logger.Information("Instruction 0x8005 called. ... v[x] -= v[y]. v[x]: {vx}", v[x]);
                            break;
                        case 0x6:
                            v[0xF] = (v[x] & 0x1);
                            Log.Logger.Information("Instruction 0x8006 called. v[0xF] = (v[x] & 0x1). v[0xF]: {v0xF}", v[0xF]);
                            break;
                        case 0x7:
                            v[0xF] = 0;

                            if (v[y] > v[x])
                            {
                                v[0xF] = 1;
                            }

                            v[x] = v[y] - v[x];
                            Log.Logger.Information("Instruction 0x8007 called. ... v[x] = v[y] - v[x]. v[x]: {vx}", v[x]);
                            break;
                        case 0xE:
                            v[0xF] = (v[x] & 0x80);
                            v[x] <<= 1;
                            Log.Logger.Information("Instruction 0x8007 called. v[0xF] = (v[x] & 0x80); v[x] <<= 1. v[0xF]: {v0xF}, v[x]: {vx}", v[0xF], v[x]);
                            break;
                    }

                    break;

                case 0x9000:
                    if (v[x] != v[y])
                    {
                        pc += 2;
                        Log.Logger.Information("Instruction 0x9000 called. if (v[x] != v[y]); pc += 2. pc: {pc}", pc);
                    }
                    break;
                case 0xA000:
                    i = (opcode & 0xFFF);
                    Log.Logger.Information("Instruction 0xA000 called. i = (opcode & 0xFFF). i: {i}", i);
                    break;
                case 0xB000:
                    pc = (opcode & 0xFFF) + v[0];
                    Log.Logger.Information("Instruction 0xB000 called. pc = (opcode & 0xFFF) + v[0];. pc: {pc}", pc);
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
                    Log.Logger.Information("Instruction 0xC000 called. ... v[x] = RANDOM & (opcode & 0xFF). v[x]: {vx}", v[x]);
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
                                SetPixel(v[x] + col, v[y] + row);
                                v[0xF] = 1;
                            }

                            sprite <<= 1;
                        }
                    }
                    Log.Logger.Information("Instruction 0xD000 called. Drawing pixels...");
                    break;
                case 0xE000:
                    switch (opcode & 0xFF)
                    {
                        case 0x9E:
                            if (IsKeyPressed(v[x]))
                            {
                                pc += 2;
                                Log.Logger.Information("Instruction 0x9E called. Pressing buttons :D");
                            }
                            break;
                        case 0xA1:
                            if (!IsKeyPressed(v[x]))
                            {
                                pc += 2;
                                Log.Logger.Information("Instruction 0xA1 called. Keyboard stuff :D");
                            }
                            break;
                    }

                    break;
                case 0xF000:
                    switch (opcode & 0xFF)
                    {
                        case 0x07:
                            v[x] = delayTimer;
                            Log.Logger.Information("Instruction 0x07 called. v[x] = delayTimer. v[x]: {vx}", v[x]);
                            break;
                        case 0x0A:
                            paused = true;

                            //keyboardstuff
                            Log.Logger.Information("Instruction 0x0A called. Pause emulator.");
                            break;
                        case 0x15:
                            delayTimer = v[x];
                            Log.Logger.Information("Instruction 0x15 called. delayTimer = v[x]. DelayTimer: {delayTimer}", delayTimer);
                            break;
                        case 0x18:
                            soundTimer = v[x];
                            Log.Logger.Information("Instruction 0x18 called. soundTimer = v[x]. SoundTimer: {soundimer}", soundTimer);
                            break;
                        case 0x1E:
                            i += v[x];
                            Log.Logger.Information("Instruction 0x1E called. i += v[x]. i: {i}", i);
                            break;
                        case 0x29:
                            i = v[x] * 5;
                            Log.Logger.Information("Instruction 0x29 called. i = v[x] * 5. i: {i}", i);
                            break;
                        case 0x33:
                            memory[i] = Convert.ToByte(v[x] / 100);

                            memory[i + 1] = Convert.ToByte(v[x] % 100 / 10);

                            memory[i + 2] = Convert.ToByte(v[x] % 10);

                            Log.Logger.Information("Instruction 0x33 called. ... memory[i + 2] = Convert.ToByte(v[x] % 10)");
                            break;
                        case 0x55:
                            for (byte registerIndex = 0; registerIndex <= x; registerIndex++)
                            {
                                memory[i + registerIndex] = Convert.ToByte(v[registerIndex]);
                                Log.Logger.Information("Instruction 0x55 called. for (byte registerIndex = 0; registerIndex <= x; registerIndex++); memory[i + registerIndex] = Convert.ToByte(v[registerIndex]).");
                            }
                            break;
                        case 0x65:
                            for (int registerIndex = 0; registerIndex <= x; registerIndex++)
                            {
                                v[registerIndex] = memory[i + registerIndex];
                                Log.Logger.Information("Instruction 0x65 called. for (int registerIndex = 0; registerIndex <= x; registerIndex++); v[registerIndex] = memory[i + registerIndex].");
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
