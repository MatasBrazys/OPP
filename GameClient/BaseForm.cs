// File: GameClient/BaseForm.cs
using System.Drawing;
using System.Windows.Forms;

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
