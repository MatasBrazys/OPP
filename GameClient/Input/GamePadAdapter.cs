//./gameclient/input/gamepadadapter.cs
using System;
using System.Drawing;
using System.Threading.Tasks;
using SharpDX.XInput;

namespace GameClient.Input.Adapters
{
    public class GamepadAdapter : IInputAdapter
    {
        private readonly Controller _controller;
        private State _currentState, _previousState;
        private readonly Point _screenCenter;
        private readonly float _deadzone = 0.15f;
        private float _aimSensitivity = 15f;
        private Point _aimPosition;

        public bool IsConnected => _controller.IsConnected;
        public string InputMethodName => $"Xbox Controller (Player {(int)_controller.UserIndex + 1})";
        public float AimSensitivity => _aimSensitivity;

        public GamepadAdapter(UserIndex playerIndex, Size screenSize)
        {
            _controller = new Controller(playerIndex);
            _screenCenter = new Point(screenSize.Width / 2, screenSize.Height / 2);
            _aimPosition = _screenCenter;
        }

        public void Update()
        {
            if (!IsConnected) return;

            _previousState = _currentState;
            _currentState = _controller.GetState();
            HandleSensitivityAdjustment();

            var (rx, ry) = GetRightStickVector();
            if (rx != 0 || ry != 0)
            {
                _aimPosition.X = Math.Clamp(_aimPosition.X + (int)(rx * _aimSensitivity), 0, _screenCenter.X * 2);
                _aimPosition.Y = Math.Clamp(_aimPosition.Y + (int)(ry * _aimSensitivity), 0, _screenCenter.Y * 2);
            }
        }

        private void HandleSensitivityAdjustment()
        {
            var current = _currentState.Gamepad.Buttons;
            var previous = _previousState.Gamepad.Buttons;

            if ((current & GamepadButtonFlags.DPadUp) != 0 && (previous & GamepadButtonFlags.DPadUp) == 0)
                _aimSensitivity = Math.Min(_aimSensitivity + 2.5f, 40f);

            if ((current & GamepadButtonFlags.DPadDown) != 0 && (previous & GamepadButtonFlags.DPadDown) == 0)
                _aimSensitivity = Math.Max(_aimSensitivity - 2.5f, 5f);
        }

        public float GetHorizontalAxis()
        {
            if (!IsConnected) return 0f;
            float val = _currentState.Gamepad.LeftThumbX / 32768f;
            return Math.Abs(val) < _deadzone ? 0f : val;
        }

        public float GetVerticalAxis()
        {
            if (!IsConnected) return 0f;
            float val = -_currentState.Gamepad.LeftThumbY / 32768f;
            return Math.Abs(val) < _deadzone ? 0f : val;
        }

        public bool IsAttackPressed()
        {
            if (!IsConnected) return false;
            return (_currentState.Gamepad.Buttons & GamepadButtonFlags.RightShoulder) != 0 &&
                   (_previousState.Gamepad.Buttons & GamepadButtonFlags.RightShoulder) == 0;
        }

        public Point GetAimPosition() => _aimPosition;

        private (float X, float Y) GetRightStickVector()
        {
            float x = _currentState.Gamepad.RightThumbX / 32768f;
            float y = -_currentState.Gamepad.RightThumbY / 32768f;
            return (Math.Abs(x) < _deadzone ? 0f : x, Math.Abs(y) < _deadzone ? 0f : y);
        }

        public void Vibrate(float leftMotor, float rightMotor, int durationMs)
        {
            if (!IsConnected) return;

            var vibration = new Vibration
            {
                LeftMotorSpeed = (ushort)(Math.Clamp(leftMotor, 0f, 1f) * 65535),
                RightMotorSpeed = (ushort)(Math.Clamp(rightMotor, 0f, 1f) * 65535)
            };
            _controller.SetVibration(vibration);
            Task.Delay(durationMs).ContinueWith(_ => _controller.SetVibration(new Vibration()));
        }
    }
}
