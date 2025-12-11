using System;

namespace GameShared.Types.Players
{
    /// <summary>
    /// Public-facing memento interface that hides the stored state.
    /// Caretakers can use metadata without seeing position data.
    /// </summary>
    public interface IPlayerMemento
    {
        string GetName();
        DateTime GetSnapshotDate();
    }
}
