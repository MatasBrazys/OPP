// File: GameClient/Player.cs
using System.Drawing;

namespace GameClient
{
    public class Player
    {
        public int Id { get; }
        public int X { get; }
        public int Y { get; }
        public Color Color { get; }

        public Player(int id, int x, int y, Color color)
        {
            Id = id;
            X = x;
            Y = y;
            Color = color;
        }

        public void Draw(Graphics g)
        {
            const int size = 30;
            g.FillEllipse(new SolidBrush(Color), X, Y, size, size);
        }
    }
}
