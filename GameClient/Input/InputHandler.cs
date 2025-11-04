// ./GameClient/Input/InputHandler.cs
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using GameShared;

namespace GameClient.Input
{
    public class InputHandler
    {
        private readonly HashSet<Keys> _pressed = new();
        public float MoveX { get; private set; }
        public float MoveY { get; private set; }
        private const float MoveSpeed = 1f;

        public void KeyDown(object? sender, KeyEventArgs e) => _pressed.Add(e.KeyCode);
        public void KeyUp(object? sender, KeyEventArgs e) => _pressed.Remove(e.KeyCode);

        public void UpdateMovement()
        {
            float dx = 0f, dy = 0f;
            if (_pressed.Contains(Keys.W)) dy -= 1f;
            if (_pressed.Contains(Keys.S)) dy += 1f;
            if (_pressed.Contains(Keys.A)) dx -= 1f;
            if (_pressed.Contains(Keys.D)) dx += 1f;

            float magnitude = (float)Math.Sqrt(dx * dx + dy * dy);
            if (magnitude > 0)
            {
                dx /= magnitude;
                dy /= magnitude;
            }

            MoveX = dx * MoveSpeed;
            MoveY = dy * MoveSpeed;
        }
    }
}
