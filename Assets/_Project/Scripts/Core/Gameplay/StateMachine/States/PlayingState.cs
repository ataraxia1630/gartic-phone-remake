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

        public override void OnEnter(GameStateMachine machine)
        {
            Debug.Log("[GameState] -> Playing");
            if (!machine.HasStateAuthority) return;

            UnsubscribeSceneLoadDone();

            var phaseManager = ServiceLocator.Get<Phases.PhaseManager>();
            if (phaseManager == null)
            {
                Debug.LogWarning("[PlayingState] PhaseManager not found, cannot start game phases");
            }

            // If DrawingTest is already active, start immediately.
            if (SceneManager.GetActiveScene().name == "DrawingTest")
            {
                phaseManager?.StartGame(machine.SelectedMode);
                return;
            }

            // Start game phases when the networked scene load has completed.
            var bootstrap = ServiceLocator.Get<NetworkBootstrap>();
            if (bootstrap != null)
            {
                _subscribedBootstrap = bootstrap;
                _onSceneLoadDone = _ =>
                {
                    if (machine.HasStateAuthority)
                    {
                        phaseManager?.StartGame(machine.SelectedMode);
                    }
                    UnsubscribeSceneLoadDone();
                };

                bootstrap.OnSceneLoadDoneEvent += _onSceneLoadDone;
            }

            // Load drawing scene for all players (host/authority triggers scene load)
            try
            {
                if (machine.Runner != null)
                {
                    machine.Runner.LoadScene("DrawingTest", UnityEngine.SceneManagement.LoadSceneMode.Single, UnityEngine.SceneManagement.LocalPhysicsMode.None, true);
                    Debug.Log("[GameState] DrawingTest scene triggered ");
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
    }
}
