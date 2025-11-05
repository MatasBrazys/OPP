// File: GameClient/Input/KeyboardMouseAdapter.cs
// FIXED VERSION with click latching

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GameClient.Input.Adapters
{
    public class KeyboardMouseAdapter : IInputAdapter
    {
        private readonly HashSet<Keys> _pressedKeys = new();
        private Point _mousePosition;
        private bool _clickPending;  // NEW: Latch to remember clicks between frames
        private readonly Form _form;

        public bool IsConnected => true;
        public string InputMethodName => "Keyboard & Mouse";

        public KeyboardMouseAdapter(Form form)
        {
            _form = form;
            _form.KeyDown += OnKeyDown;
            _form.KeyUp += OnKeyUp;
            _form.MouseDown += OnMouseDown;
            _form.MouseUp += OnMouseUp;
            _form.MouseMove += OnMouseMove;
            
            Console.WriteLine("[KeyboardMouseAdapter] Initialized with latched click detection");
        }

        public void Update()
        {
            // No need to track previous state anymore
            // The latch persists until we check it
        }

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
            // Check if there's a pending click, then clear it
            if (_clickPending)
            {
                _clickPending = false;
                Console.WriteLine("[KeyboardMouseAdapter] Attack pressed - consuming latched click");
                return true;
            }
            return false;
        }

        public Point GetAimPosition()
        {
            return _mousePosition;
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            _pressedKeys.Add(e.KeyCode);
        }

        private void OnKeyUp(object? sender, KeyEventArgs e)
        {
            _pressedKeys.Remove(e.KeyCode);
        }

        private void OnMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _clickPending = true;  // Latch the click
                Console.WriteLine($"[KeyboardMouseAdapter] Click latched at ({e.X}, {e.Y})");
            }
        }

        private void OnMouseUp(object? sender, MouseEventArgs e)
        {
            // We don't clear the latch here - it persists until IsAttackPressed() consumes it
        }

        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            _mousePosition = e.Location;
        }

        public void Cleanup()
        {
            _form.KeyDown -= OnKeyDown;
            _form.KeyUp -= OnKeyUp;
            _form.MouseDown -= OnMouseDown;
            _form.MouseUp -= OnMouseUp;
            _form.MouseMove -= OnMouseMove;
        }
    }
}