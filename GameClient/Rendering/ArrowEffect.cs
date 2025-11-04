using System;
using System.Drawing;

namespace GameClient.Rendering
{
    public class ArrowEffect
    {
        private readonly float _startX, _startY, _endX, _endY;
        private readonly float _angle;
        private readonly float _speed = 800f; // px/s
        private readonly DateTime _startTime;
        private readonly float _distance;
        private readonly float _duration;
        public bool IsFinished { get; private set; }

        public ArrowEffect(float startX, float startY, float endX, float endY, float angleDeg)
        {
            _startX = startX;
            _startY = startY;
            _endX = endX;
            _endY = endY;
            _angle = angleDeg;
            _distance = (float)Math.Sqrt(Math.Pow(_endX - _startX, 2) + Math.Pow(_endY - _startY, 2));
            _duration = _distance / _speed;
            _startTime = DateTime.UtcNow;
        }

        public void Draw(Graphics g)
        {
            double elapsed = (DateTime.UtcNow - _startTime).TotalSeconds;
            if (elapsed > _duration)
            {
                IsFinished = true;
                return;
            }

            float t = (float)(elapsed / _duration);
            float x = _startX + (_endX - _startX) * t;
            float y = _startY + (_endY - _startY) * t;

            // Arrow style
            float length = 20f;
            float width = 3f;

            using var pen = new Pen(Color.DarkGreen, width);
            g.TranslateTransform(x, y);
            g.RotateTransform(_angle);
            g.DrawLine(pen, 0, 0, length, 0);
            g.ResetTransform();
        }
    }
}
