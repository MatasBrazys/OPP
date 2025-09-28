using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;

namespace GameClient
{
    public partial class GameClientForm : BaseForm
    {
        private TcpClient? client;
        private NetworkStream? stream;
        private Thread? receiveThread;
        private Dictionary<int, Player> players = new Dictionary<int, Player>();
        private Player? myPlayer;
        private bool up, down, left, right;
        private System.Windows.Forms.Timer gameTimer;

        public GameClientForm()
        {
            myPlayer = new Player(0, 200, 200, Color.Blue);
            players[myPlayer.Id] = myPlayer;

            gameTimer = new System.Windows.Forms.Timer();
            gameTimer.Interval = 30;
            gameTimer.Tick += GameLoop;
            gameTimer.Start();

            this.KeyDown += GameClientForm_KeyDown;
            this.KeyUp += GameClientForm_KeyUp;
            this.Paint += GameClientForm_Paint;

            // Load event for network connection
            this.Load += GameClientForm_Load;
        }

        private void GameClientForm_Load(object? sender, EventArgs e)
        {
            try
            {
                client = new TcpClient("127.0.0.1", 5000);
                stream = client.GetStream();

                receiveThread = new Thread(ReceiveData);
                receiveThread.IsBackground = true;
                receiveThread.Start();
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

                if (stream != null)
                {
                    var msg = new NetworkMessage { Id = myPlayer.Id, X = myPlayer.X, Y = myPlayer.Y };
                    string json = JsonSerializer.Serialize(msg)+ "\n"; // Add newline as a delimiter
                    byte[] data = Encoding.UTF8.GetBytes(json);
                    try { stream.Write(data, 0, data.Length); } catch { }
                }
            }

            Invalidate();
        }

        private void ReceiveData()
        {
            try
            {
                if (stream == null) return;

                using var reader = new StreamReader(stream, Encoding.UTF8);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    var states = JsonSerializer.Deserialize<List<NetworkMessage>>(line);
                    if (states == null) continue;

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
            }
            catch
            {
                // disconnected
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
