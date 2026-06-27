using InkEcho.Network.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;

namespace InkEcho.Network.StateMachine.States
{
    public class ResultsState : GameStateBase
    {
        public override GameStateType Type => GameStateType.Results;

        public override void OnEnter(GameStateMachine machine)
        {
            Debug.Log("[GameState] -> Results (Bắt đầu Reveal Album)");
            if (!machine.HasStateAuthority) return;

            machine.RevealAlbumIndex = 0;
            machine.RevealLinkIndex = 0;
            machine.IsRevealFinished = false;
            machine.ResultsTimer = TickTimer.None;

            // Yêu cầu tất cả client gửi lại tranh về host trước khi reveal.
            var registry = ServiceLocator.Get<Players.PlayerRegistry>();
            registry?.Rpc_RequestRebroadcastToHost(machine.Runner.LocalPlayer);

            try
            {
                if (machine.Runner != null)
                    machine.Runner.LoadScene("ResultScene", LoadSceneMode.Single, LocalPhysicsMode.None, true);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ResultsState] Đổi scene thất bại: {ex.Message}");
            }
        }

        // Reveal được host điều khiển thủ công qua Rpc_RevealNext(). Không cần tick.
        public override void Tick(GameStateMachine machine) { }

        public override void OnExit(GameStateMachine machine)
        {
            if (!machine.HasStateAuthority) return;

            var registry = ServiceLocator.Get<Players.PlayerRegistry>();
            registry?.ResetReadyFlags();

            var phaseManager = ServiceLocator.Get<Phases.PhaseManager>();
            phaseManager?.ResetForLobby();
        }
    }
}