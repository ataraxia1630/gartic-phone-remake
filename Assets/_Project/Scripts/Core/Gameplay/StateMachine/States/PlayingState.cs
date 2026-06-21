using InkEcho.Network.Core;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InkEcho.Network.StateMachine.States
{
    public class PlayingState : GameStateBase
    {
        private System.Action<NetworkRunner> _onSceneLoadDone;
        private NetworkBootstrap _subscribedBootstrap;

        public override GameStateType Type => GameStateType.Playing;

        private const string GameSceneName = "BasePhase";

        public override void OnEnter(GameStateMachine machine)
        {
            Debug.Log("[GameState] -> Playing");
            if (!machine.HasStateAuthority) return;

            // Game đã bắt đầu: đóng session, chặn người mới cho đến khi quay lại sảnh.
            if (machine.Runner != null && machine.Runner.SessionInfo != null && machine.Runner.SessionInfo.IsValid)
            {
                machine.Runner.SessionInfo.IsOpen = false;
                machine.Runner.SessionInfo.IsVisible = false;
                Debug.Log("[PlayingState] Session đóng — chặn người mới vào.");
            }

            UnsubscribeSceneLoadDone();

            if (SceneManager.GetActiveScene().name == GameSceneName)
            {
                Debug.LogWarning("[PlayingState] PhaseManager not found, cannot start game phases");
            }

            // If BasePhase is already active, start immediately.
            if (SceneManager.GetActiveScene().name == "BasePhase")
            {
                var pm = ResolvePhaseManager();
                if (pm != null)
                    pm.StartGame(machine.SelectedMode);
                else
                    Debug.LogError("[PlayingState] PhaseManager NULL — cannot start game phases");
                return;
            }

            var bootstrap = ServiceLocator.Get<NetworkBootstrap>();
            if (bootstrap != null)
            {
                _subscribedBootstrap = bootstrap;
                _onSceneLoadDone = _ =>
                {
                    if (machine.HasStateAuthority)
                    {
                        var pm = ResolvePhaseManager();
                        if (pm != null)
                            pm.StartGame(machine.SelectedMode);
                        else
                            Debug.LogError("[PlayingState] PhaseManager NULL after scene load — StartGame skipped!");
                    }
                    UnsubscribeSceneLoadDone();
                };
                bootstrap.OnSceneLoadDoneEvent += _onSceneLoadDone;
            }

            try
            {
                if (machine.Runner != null)
                {
                    machine.Runner.LoadScene(GameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single, UnityEngine.SceneManagement.LocalPhysicsMode.None, true);
                    Debug.Log($"[GameState] Loading {GameSceneName}");
                }
            }
            catch (System.Exception ex)
            {
                UnsubscribeSceneLoadDone();
                Debug.LogWarning($"[PlayingState] LoadScene failed: {ex.Message}");
            }
        }

        public override void OnExit(GameStateMachine machine)
        {
            UnsubscribeSceneLoadDone();
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

        private void UnsubscribeSceneLoadDone()
        {
            if (_subscribedBootstrap != null && _onSceneLoadDone != null)
            {
                _subscribedBootstrap.OnSceneLoadDoneEvent -= _onSceneLoadDone;
            }

            _subscribedBootstrap = null;
            _onSceneLoadDone = null;
        }

        private static Phases.PhaseManager ResolvePhaseManager()
        {
            var pm = ServiceLocator.Get<Phases.PhaseManager>();
            if (pm != null) return pm;

            // Fallback: ServiceLocator might not have re-registered after scene change
            pm = Object.FindAnyObjectByType<Phases.PhaseManager>();
            if (pm != null)
            {
                ServiceLocator.Register<Phases.PhaseManager>(pm);
                Debug.Log("[PlayingState] PhaseManager found via FindObjectOfType — re-registered in ServiceLocator");
            }
            return pm;
        }
    }
}
