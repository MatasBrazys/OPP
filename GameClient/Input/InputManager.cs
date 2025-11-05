//./gameclient/input/inputmanager.cs
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
            _adapters.Add(new KeyboardMouseAdapter(form));

            for (int i = 0; i < 4; i++)
                _adapters.Add(new GamepadAdapter((UserIndex)i, screenSize));

            _activeAdapter = _adapters[0];
        }

        public void Update()
        {
            foreach (var adapter in _adapters) adapter.Update();
            DetectActiveInput();
        }

        public void CycleInputAdapter()
        {
            do
            {
                _activeIndex = (_activeIndex + 1) % _adapters.Count;
                _activeAdapter = _adapters[_activeIndex];
            } while (!_activeAdapter.IsConnected && _activeIndex != 0);
        }

        private void DetectActiveInput()
        {
            foreach (var adapter in _adapters)
            {
                if (!adapter.IsConnected) continue;
                if (Math.Abs(adapter.GetHorizontalAxis()) > 0.1f || Math.Abs(adapter.GetVerticalAxis()) > 0.1f)
                {
                    _activeAdapter = adapter;
                    break;
                }
            }
        }

        public (float dx, float dy) GetMovementInput()
        {
            if (_activeAdapter == null || !_activeAdapter.IsConnected) return (0, 0);

            float dx = _activeAdapter.GetHorizontalAxis();
            float dy = _activeAdapter.GetVerticalAxis();
            float mag = (float)Math.Sqrt(dx * dx + dy * dy);
            if (mag > 1f) { dx /= mag; dy /= mag; }

            return (dx, dy);
        }

        public bool IsAttackPressed() => _activeAdapter?.IsAttackPressed() ?? false;
        public Point GetAimPosition() => _activeAdapter?.GetAimPosition() ?? Point.Empty;
    }
}
