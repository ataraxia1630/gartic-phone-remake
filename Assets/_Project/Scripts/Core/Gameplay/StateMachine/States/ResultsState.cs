using Fusion;
using InkEcho.Network.Core;
using InkEcho.Network.Data;
using InkEcho.Network.Phases;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            machine.IsRevealFinished = false;
            ResetResultsTimer(machine);

            try
            {
                if (machine.Runner != null)
                {
                    machine.Runner.LoadScene("ResultScene", LoadSceneMode.Single, LocalPhysicsMode.None, true);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ResultsState] Đổi scene thất bại: {ex.Message}");
            }
        }

        public override void Tick(GameStateMachine machine)
        {
            if (!machine.HasStateAuthority) return;
            if (machine.IsRevealFinished) return;

            if (machine.ResultsTimer.Expired(machine.Runner))
            {
                var albumStore = ServiceLocator.Get<AlbumStore>();
                byte totalAlbums = albumStore != null ? albumStore.PlayerCount : (byte)0;
                int nextIndex = machine.RevealAlbumIndex + 1;

                if (totalAlbums == 0 || nextIndex >= totalAlbums)
                {
                    machine.IsRevealFinished = true;
                    machine.ResultsTimer = TickTimer.None;
                    Debug.Log("[ResultsState] Đã reveal xong toàn bộ Album!");
                    return;
                }

                machine.RevealAlbumIndex = (byte)nextIndex;
                ResetResultsTimer(machine);
            }
        }

        public override void OnExit(GameStateMachine machine)
        {
            if (!machine.HasStateAuthority) return;

            var registry = ServiceLocator.Get<Players.PlayerRegistry>();
            registry?.ResetSubmittedFlags();

            var phaseManager = ServiceLocator.Get<Phases.PhaseManager>();
            phaseManager?.ResetForLobby();
        }

        private void ResetResultsTimer(GameStateMachine machine)
        {
            var phaseManager = ServiceLocator.Get<PhaseManager>();
            float seconds = phaseManager != null ? phaseManager.ResolveDuration(PhaseType.Reveal) : 8f;

            if (seconds > 0f)
                machine.ResultsTimer = TickTimer.CreateFromSeconds(machine.Runner, seconds);
        }
    }
}