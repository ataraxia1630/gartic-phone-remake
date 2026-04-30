using GarticPhone.Network.Core;
using UnityEngine;

namespace GarticPhone.Network.StateMachine.States
{
    public class ResultsState : GameStateBase
    {
        public override GameStateType Type => GameStateType.Results;

        public override void OnEnter(GameStateMachine machine)
        {
            Debug.Log("[GameState] -> Results");
        }

        public override void OnExit(GameStateMachine machine)
        {
            if (!machine.HasStateAuthority) return;

            var registry = ServiceLocator.Get<Players.PlayerRegistry>();
            registry?.ResetSubmittedFlags();

            var phaseManager = ServiceLocator.Get<Phases.PhaseManager>();
            phaseManager?.ResetForLobby();
        }
    }
}
