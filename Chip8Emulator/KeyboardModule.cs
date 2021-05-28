using SFML.Window;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8Emulator
{
    public class KeyboardModule : IKeyboardModule
    {

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

        private int[] keysPressed;

        public int IsKeyPressed(int keyCode)
        {
            return keysPressed[keyCode];
        }
    }
}
