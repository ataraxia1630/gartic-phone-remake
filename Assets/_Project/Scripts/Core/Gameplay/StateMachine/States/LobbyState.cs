using InkEcho.Network.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InkEcho.Network.StateMachine.States
{
    public class LobbyState : GameStateBase
    {
        public override GameStateType Type => GameStateType.Lobby;

        private const string LobbySceneName = "NetworkSystem_Test";

        public override void OnEnter(GameStateMachine machine)
        {
            Debug.Log("[GameState] -> Lobby");

            if (!machine.HasStateAuthority) return;

            // Nếu đang ở scene khác (vd ResultScene), load lại Lobby scene
            if (SceneManager.GetActiveScene().name != LobbySceneName)
            {
                try
                {
                    if (machine.Runner != null)
                    {
                        machine.Runner.LoadScene(LobbySceneName, LoadSceneMode.Single, LocalPhysicsMode.None, true);
                        Debug.Log($"[LobbyState] Loading {LobbySceneName}");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[LobbyState] LoadScene failed: {ex.Message}");
                }
            }
        }
    }
}
