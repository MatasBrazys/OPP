// File: GameClient/Input/GamepadAdapter.cs - UPDATED VERSION

using System;
using System.Drawing;
using SharpDX.XInput;

namespace GameClient.Input.Adapters
{
    public class GamepadAdapter : IInputAdapter
    {
        private readonly Controller _controller;
        private State _currentState;
        private State _previousState;
        private readonly Point _screenCenter;
        private readonly float _deadzone = 0.15f;
        
        // ADJUSTABLE SENSITIVITY
        private float _aimSensitivity = 15f;  // Default speed
        private const float MinSensitivity = 5f;
        private const float MaxSensitivity = 40f;
        private const float SensitivityStep = 2.5f;
        
        private Point _aimPosition;

        public bool IsConnected => _controller.IsConnected;
        public string InputMethodName => $"Xbox Controller (Player {(int)_controller.UserIndex + 1})";
        
        // Expose sensitivity for UI display
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

            // Check for sensitivity adjustment (D-Pad Up/Down)
            HandleSensitivityAdjustment();

            // Update aim cursor position using right stick
            var rightStick = GetRightStickVector();
            if (rightStick.X != 0 || rightStick.Y != 0)
            {
                _aimPosition.X += (int)(rightStick.X * _aimSensitivity);
                _aimPosition.Y += (int)(rightStick.Y * _aimSensitivity);

                // Clamp to screen bounds
                _aimPosition.X = Math.Clamp(_aimPosition.X, 0, _screenCenter.X * 2);
                _aimPosition.Y = Math.Clamp(_aimPosition.Y, 0, _screenCenter.Y * 2);
            }
        }

        private void HandleSensitivityAdjustment()
        {
            var currentButtons = _currentState.Gamepad.Buttons;
            var previousButtons = _previousState.Gamepad.Buttons;

            // D-Pad Up: Increase sensitivity
            bool dpadUpPressed = (currentButtons & GamepadButtonFlags.DPadUp) != 0 &&
                                (previousButtons & GamepadButtonFlags.DPadUp) == 0;
            
            if (dpadUpPressed && _aimSensitivity < MaxSensitivity)
            {
                _aimSensitivity = Math.Min(_aimSensitivity + SensitivityStep, MaxSensitivity);
                Console.WriteLine($"[Gamepad] Aim sensitivity increased to {_aimSensitivity:F1}");
            }

            // D-Pad Down: Decrease sensitivity
            bool dpadDownPressed = (currentButtons & GamepadButtonFlags.DPadDown) != 0 &&
                                  (previousButtons & GamepadButtonFlags.DPadDown) == 0;
            
            if (dpadDownPressed && _aimSensitivity > MinSensitivity)
            {
                _aimSensitivity = Math.Max(_aimSensitivity - SensitivityStep, MinSensitivity);
                Console.WriteLine($"[Gamepad] Aim sensitivity decreased to {_aimSensitivity:F1}");
            }
        }

        public float GetHorizontalAxis()
        {
            if (!IsConnected) return 0f;

            var gamepad = _currentState.Gamepad;
            float value = gamepad.LeftThumbX / 32768f;

            return Math.Abs(value) < _deadzone ? 0f : value;
        }

        public float GetVerticalAxis()
        {
            if (!IsConnected) return 0f;

            var gamepad = _currentState.Gamepad;
            float value = -gamepad.LeftThumbY / 32768f;

            return Math.Abs(value) < _deadzone ? 0f : value;
        }

        public bool IsAttackPressed()
        {
            if (!IsConnected) return false;

            var wasPressed = (_previousState.Gamepad.Buttons & GamepadButtonFlags.RightShoulder) != 0;
            var isPressed = (_currentState.Gamepad.Buttons & GamepadButtonFlags.RightShoulder) != 0;

            return isPressed && !wasPressed;
        }

        public Point GetAimPosition()
        {
            return _aimPosition;
        }

        private (float X, float Y) GetRightStickVector()
        {
            var gamepad = _currentState.Gamepad;
            float x = gamepad.RightThumbX / 32768f;
            float y = -gamepad.RightThumbY / 32768f;

            if (Math.Abs(x) < _deadzone) x = 0f;
            if (Math.Abs(y) < _deadzone) y = 0f;

            return (x, y);
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

            Task.Delay(durationMs).ContinueWith(_ =>
            {
                _controller.SetVibration(new Vibration());
            });
        }
    }
}