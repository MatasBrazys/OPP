using System.Drawing;
using System.Windows.Forms;

namespace GameClient;

public class BaseForm : Form
    {
        public BaseForm()
        {
            this.Text = "Game Client";
            this.ClientSize = new Size(800, 450);
            this.DoubleBuffered = true;
        }
    }
