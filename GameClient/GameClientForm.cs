using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GameClient
{
    public partial class GameClientForm : BaseForm
    {
        private readonly NetworkManager network;
        private Dictionary<int, Player> players = new();
        private Player? myPlayer;

        private bool up, down, left, right;
        private System.Windows.Forms.Timer gameTimer;

        public GameClientForm()
        {
            network = new NetworkManager();
            network.OnGameStateReceived += HandleGameState;

            myPlayer = new Player(0, 200, 200, Color.Blue);
            players[myPlayer.Id] = myPlayer;

            gameTimer = new System.Windows.Forms.Timer();
            gameTimer.Interval = 30;
            gameTimer.Tick += GameLoop;
            gameTimer.Start();

            this.KeyDown += GameClientForm_KeyDown;
            this.KeyUp += GameClientForm_KeyUp;
            this.Paint += GameClientForm_Paint;

            this.Load += GameClientForm_Load;
        }

        private void GameClientForm_Load(object? sender, EventArgs e)
        {
            try
            {
                // TODO: padaryti langelį, kad įvestum serverio IP vietoj hardcoded
                string serverIp = "25.55.216.17"; 
                int port = 5000;
                network.Connect(serverIp, port);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connection failed: " + ex.Message);
            }
        }

        private void GameLoop(object? sender, EventArgs e)
        {
            if (myPlayer != null)
            {
                if (up) myPlayer.Y -= 5;
                if (down) myPlayer.Y += 5;
                if (left) myPlayer.X -= 5;
                if (right) myPlayer.X += 5;

                if (network.IsConnected)
                {
                    var msg = new NetworkMessage { Id = myPlayer.Id, X = myPlayer.X, Y = myPlayer.Y };
                    network.Send(msg);
                }
            }

            Invalidate();
        }

        private void HandleGameState(List<NetworkMessage> states)
        {
            lock (players)
            {
                players.Clear();
                foreach (var st in states)
                {
                    if (myPlayer == null || myPlayer.Id == 0)
                    {
                        myPlayer = new Player(st.Id, st.X, st.Y, Color.Blue);
                    }

                    Color c = st.Id == myPlayer.Id ? Color.Blue : Color.Red;
                    players[st.Id] = new Player(st.Id, st.X, st.Y, c);
                }
            }
        }

        private void GameClientForm_Paint(object? sender, PaintEventArgs e)
        {
            lock (players)
            {
                foreach (var p in players.Values)
                {
                    p.Draw(e.Graphics);
                }
            }
        }

        private void GameClientForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W) up = true;
            if (e.KeyCode == Keys.S) down = true;
            if (e.KeyCode == Keys.A) left = true;
            if (e.KeyCode == Keys.D) right = true;
        }

        private void GameClientForm_KeyUp(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W) up = false;
            if (e.KeyCode == Keys.S) down = false;
            if (e.KeyCode == Keys.A) left = false;
            if (e.KeyCode == Keys.D) right = false;
        }
    }
}
