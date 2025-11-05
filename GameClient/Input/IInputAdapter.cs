
// File: GameClient/Input/IInputAdapter.cs
// ============================================================================
// ADAPTER PATTERN: Input System
// ============================================================================
namespace GameClient.Input.Adapters
{

    public interface IInputAdapter
    {
        void Update();
        float GetHorizontalAxis();
        float GetVerticalAxis();
        bool IsAttackPressed();
        Point GetAimPosition();
        bool IsConnected { get; }
        string InputMethodName { get; }
    }
}
