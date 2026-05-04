using UnityEngine;

namespace InkEcho.Network.StateMachine.States
{
    public class LobbyState : GameStateBase
    {
        public override GameStateType Type => GameStateType.Lobby;

        public override void OnEnter(GameStateMachine machine)
        {
            Debug.Log("[GameState] -> Lobby");
        }
    }
}
