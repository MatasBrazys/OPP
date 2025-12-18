using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameClient.States
{
    public abstract class PlayerStateBase
    {
        protected readonly PlayerStateManager _stateManager;

        protected PlayerStateBase(PlayerStateManager stateManager)
        {
            _stateManager = stateManager;
        }

        public abstract string StateName { get; }

        // Default behavior: ignore input
        public virtual void HandleMovement(int dx, int dy) { }
        public virtual void HandleAttack(int aimX, int aimY) { }
        public virtual void HandlePlant() { }
        public virtual void HandleHarvest() { }
        public virtual void HandleSleepToggle() { }
        public virtual void HandleTaskAssignment() { }
        public virtual void HandleTaskCompletion() { }

        // Called each frame while in this state
        public virtual void Update() { }

        // Optional lifecycle hooks
        public virtual void OnEnter() { }
        public virtual void OnExit() { }
    }
}
