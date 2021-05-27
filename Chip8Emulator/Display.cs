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
        static RenderWindow win;
        public void Render()
        {
            RenderWindow win = new RenderWindow(new SFML.Window.VideoMode(32, 64), "Chip 8 Emulator");
            win.SetVerticalSyncEnabled(true);

            win.Closed += Win_Closed;

            while (win.IsOpen)
            {
                win.DispatchEvents();
                win.Draw(SetPixel(0, 0));
                win.Display();
                //win.Clear(Color.Black);
            }
        }

        public RectangleShape SetPixel(float x, float y)
        {
            RectangleShape pixel = new RectangleShape
            {
                Size = new Vector2f(1, 1),
                FillColor = Color.Red,
                Position = new Vector2f(x, y)
            };

            return pixel;
        }

        private static void Win_Closed(object sender, EventArgs e)
        {
            win.Close();
        }
    }
}
