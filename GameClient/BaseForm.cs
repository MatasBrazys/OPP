// File: GameClient/BaseForm.cs

namespace GameClient
{
    public class BaseForm : Form
    {
        public BaseForm()
        {
            Text = "Game Client";
            ClientSize = new Size(1920,1024 );
            DoubleBuffered = true;
        }
    }
}
