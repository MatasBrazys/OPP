// File: GameClient/BaseForm.cs

namespace GameClient
{
    public class BaseForm : Form
    {
        public BaseForm()
        {
            Text = "Game Client";
            ClientSize = new Size(800, 450);
            DoubleBuffered = true;
        }
    }
}
