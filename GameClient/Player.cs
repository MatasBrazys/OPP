using System.Drawing;

namespace GameClient
{
    public class Player
    {
        public int Id { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Size { get; } = 30;
        public Color Color { get; set; }

        public Player(int id, int x, int y, Color color)
        {
            Id = id;
            X = x;
            Y = y;
            Color = color;
        }

        public void Draw(Graphics g)
        {
            using (Brush b = new SolidBrush(Color))
            {
                g.FillRectangle(b, X, Y, Size, Size);
            }
        }
    }
}
