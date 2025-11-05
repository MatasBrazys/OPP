//./gameclient/input/keyboardmouseadapter.cs
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GameClient.Input.Adapters
{
    public class KeyboardMouseAdapter : IInputAdapter
    {
        private readonly HashSet<Keys> _pressedKeys = new();
        private Point _mousePosition;
        private bool _clickPending;
        private readonly Form _form;

        public bool IsConnected => true;
        public string InputMethodName => "Keyboard & Mouse";

        public KeyboardMouseAdapter(Form form)
        {
            _form = form;
            _form.KeyDown += OnKeyDown;
            _form.KeyUp += OnKeyUp;
            _form.MouseDown += OnMouseDown;
            _form.MouseMove += OnMouseMove;
        }

        public void Update() { }

        public float GetHorizontalAxis()
        {
            float axis = 0f;
            if (_pressedKeys.Contains(Keys.A)) axis -= 1f;
            if (_pressedKeys.Contains(Keys.D)) axis += 1f;
            return axis;
        }

        public float GetVerticalAxis()
        {
            float axis = 0f;
            if (_pressedKeys.Contains(Keys.W)) axis -= 1f;
            if (_pressedKeys.Contains(Keys.S)) axis += 1f;
            return axis;
        }

        public bool IsAttackPressed()
        {
            if (_clickPending)
            {
                _clickPending = false;
                return true;
            }
            return false;
        }

        public Point GetAimPosition() => _mousePosition;

        private void OnKeyDown(object? sender, KeyEventArgs e) => _pressedKeys.Add(e.KeyCode);
        private void OnKeyUp(object? sender, KeyEventArgs e) => _pressedKeys.Remove(e.KeyCode);
        private void OnMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) _clickPending = true;
        }
        private void OnMouseMove(object? sender, MouseEventArgs e) => _mousePosition = e.Location;

        public void Cleanup()
        {
            _form.KeyDown -= OnKeyDown;
            _form.KeyUp -= OnKeyUp;
            _form.MouseDown -= OnMouseDown;
            _form.MouseMove -= OnMouseMove;
        }
    }
}
