using SFML.Graphics;
using SFML.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chip8Emulator
{
    public class Display : IDisplay
    {
        private static RenderWindow win = new RenderWindow(new SFML.Window.VideoMode(32, 64), "Chip 8 Emulator");

        public void Render()
        {
            win.SetVerticalSyncEnabled(true);
            win.SetFramerateLimit(60);
            win.Closed += Win_Closed;

            while (win.IsOpen)
            {
                win.DispatchEvents();
                win.Clear(Color.Black);
                win.Display();
            }
        }

        public bool SetPixel(int x, int y)
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

            int pixelLoc = x + (y + cols);

            RectangleShape pixel = new RectangleShape
            {
                Size = new Vector2f(1, 1),
                FillColor = Color.White,
                Position = new Vector2f(x, y)
            };

            win.Draw(pixel);

            return true;
        }

        public void Clear()
        {
            win.Clear(Color.Black);
        }

        private static void Win_Closed(object sender, EventArgs e)
        {
            win.Close();
        }
    }
}
