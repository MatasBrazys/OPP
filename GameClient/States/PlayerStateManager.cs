using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameClient.States
{
    public class PlayerStateManager
    {
        public PlayerStateBase CurrentState { get; private set; }

        // States
        private readonly ActiveState _activeState;
        private readonly AttackState _attackState;
        private readonly SleepState _sleepState;
        private readonly WorkingState _workingState;

        public PlayerStateManager(
            ActiveState active,
            AttackState attack,
            SleepState sleep,
            WorkingState working)
        {
            _activeState = active;
            _attackState = attack;
            _sleepState = sleep;
            _workingState = working;

            ChangeState(_activeState);
        }

        private void ChangeState(PlayerStateBase newState)
        {
            CurrentState?.OnExit();
            CurrentState = newState;
            CurrentState.OnEnter();

            Console.WriteLine($"[STATE] Changed to {CurrentState.StateName}");
        }

        // Transitions
        public void TransitionToActive() => ChangeState(_activeState);
        public void TransitionToAttack() => ChangeState(_attackState);
        public void TransitionToSleep() => ChangeState(_sleepState);
        public void TransitionToWorking() => ChangeState(_workingState);

        /// <summary>
        /// Gets the working state for setting task details before transitioning.
        /// </summary>
        public WorkingState GetWorkingState() => _workingState;
    }
}
