using System.Drawing;

public class Player
{
    public int Id { get; }
    public int X { get; } // tile coordinates
    public int Y { get; } // tile coordinates
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
    int size = 30;
    // Server already sends pixel X, Y → no scaling with tileSize
    g.FillEllipse(new SolidBrush(Color), X, Y, size, size);
}
}
