using GarticPhone.Network.Core;
using UnityEngine;

namespace GarticPhone.Network.StateMachine.States
{
    public class PlayingState : GameStateBase
    {
        public override GameStateType Type => GameStateType.Playing;

        public override void OnEnter(GameStateMachine machine)
        {
            Debug.Log("[GameState] -> Playing");
            if (!machine.HasStateAuthority) return;

            var phaseManager = ServiceLocator.Get<Phases.PhaseManager>();
            phaseManager?.StartGame(machine.SelectedMode);
        }

        public override void Tick(GameStateMachine machine)
        {
            if (!machine.HasStateAuthority) return;

            var phaseManager = ServiceLocator.Get<Phases.PhaseManager>();
            if (phaseManager != null && phaseManager.IsGameFinished)
            {
                machine.Transition(GameStateType.Results);
            }
        }
    }
}
