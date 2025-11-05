// File: GameClient/Input/InputManager.cs

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SharpDX.XInput;

namespace GameClient.Input.Adapters
{
    public class InputManager
    {
        private readonly List<IInputAdapter> _adapters = new();
        private IInputAdapter _activeAdapter;
        private int _activeIndex = 0;

        public IInputAdapter ActiveAdapter => _activeAdapter;
        public string CurrentInputMethod => _activeAdapter?.InputMethodName ?? "None";

        public InputManager(Form form, Size screenSize)
        {
            // Add keyboard/mouse adapter
            _adapters.Add(new KeyboardMouseAdapter(form));

            // Try to add up to 4 controller adapters
            for (int i = 0; i < 4; i++)
            {
                var adapter = new GamepadAdapter((UserIndex)i, screenSize);
                _adapters.Add(adapter);
            }

            // Set first connected adapter as active
            _activeAdapter = _adapters[0];
            _activeIndex = 0;
        }

        public void Update()
        {
            foreach (var adapter in _adapters)
            {
                adapter.Update();
            }

            // Auto-switch to the adapter that's providing input
            DetectActiveInput();
        }

        public void CycleInputAdapter()
        {
            do
            {
                _activeIndex = (_activeIndex + 1) % _adapters.Count;
                _activeAdapter = _adapters[_activeIndex];
            } while (!_activeAdapter.IsConnected && _activeIndex != 0);

            Console.WriteLine($"[INPUT] Switched to: {_activeAdapter.InputMethodName}");
        }

        private void DetectActiveInput()
        {
            foreach (var adapter in _adapters)
            {
                if (!adapter.IsConnected) continue;

                // Check if this adapter is providing input
                // IMPORTANT: Don't call IsAttackPressed() here - it consumes the click!
                // Only check movement input for auto-switching
                if (Math.Abs(adapter.GetHorizontalAxis()) > 0.1f ||
                    Math.Abs(adapter.GetVerticalAxis()) > 0.1f)
                {
                    if (_activeAdapter != adapter)
                    {
                        _activeAdapter = adapter;
                        Console.WriteLine($"[INPUT] Auto-switched to: {_activeAdapter.InputMethodName}");
                    }
                    break;
                }
            }
        }

        public (float dx, float dy) GetMovementInput()
        {
            if (_activeAdapter == null || !_activeAdapter.IsConnected)
                return (0, 0);

            float dx = _activeAdapter.GetHorizontalAxis();
            float dy = _activeAdapter.GetVerticalAxis();

            // Normalize diagonal movement
            float magnitude = (float)Math.Sqrt(dx * dx + dy * dy);
            if (magnitude > 1f)
            {
                dx /= magnitude;
                dy /= magnitude;
            }

            return (dx, dy);
        }

        public bool IsAttackPressed()
        {
            return _activeAdapter?.IsAttackPressed() ?? false;
        }

        public Point GetAimPosition()
        {
            return _activeAdapter?.GetAimPosition() ?? Point.Empty;
        }

        public List<string> GetConnectedDevices()
        {
            var devices = new List<string>();
            foreach (var adapter in _adapters)
            {
                if (adapter.IsConnected)
                    devices.Add(adapter.InputMethodName);
            }
            return devices;
        }
    }
}